using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Middleware.LoggingExtensions;
using FunFair.Common.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BuildBot.GitHub.Middleware;

public sealed class GitHubWebhookSignatureMiddleware : IMiddleware
{
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";
    private const string DELIVERY_HEADER = "X-GitHub-Delivery";
    private const string EVENT_HEADER = "X-GitHub-Event";

    // Typical push payloads are well under this, so they stay in memory rather than spilling to a temp file.
    private const int BUFFER_THRESHOLD = 1024 * 1024;

    // GitHub caps webhook payloads at 25 MB, so anything larger cannot be a genuine delivery.
    private const long BUFFER_LIMIT = 25L * 1024 * 1024;

    private readonly ILogger<GitHubWebhookSignatureMiddleware> _logger;
    private readonly IRemoteIpAddressRetriever _remoteIpAddressRetriever;
    private readonly IWebhookSignatureVerifier _verifier;

    public GitHubWebhookSignatureMiddleware(
        IWebhookSignatureVerifier verifier,
        IRemoteIpAddressRetriever remoteIpAddressRetriever,
        ILogger<GitHubWebhookSignatureMiddleware> logger
    )
    {
        this._verifier = verifier;
        this._remoteIpAddressRetriever = remoteIpAddressRetriever;
        this._logger = logger;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Runs for every request, so only endpoints the routes mark as GitHub webhooks pay for verification.
        if (context.GetEndpoint()?.Metadata.GetMetadata<GitHubWebhookEndpointMetadata>() is null)
        {
            return next(context);
        }

        return this.VerifyThenInvokeAsync(context: context, next: next);
    }

    private async Task VerifyThenInvokeAsync(HttpContext context, RequestDelegate next)
    {
        HttpRequest request = context.Request;
        StringValues signatures = request.Headers[SIGNATURE_HEADER];

        // Only read the body once there is a single signature to check it against.
        WebhookSignatureVerificationResult result = signatures.Count switch
        {
            0 => WebhookSignatureVerificationResult.MISSING_HEADER,
            1 => await this.VerifyBodyAsync(
                request: request,
                signatureHeader: signatures.ToString(),
                cancellationToken: context.RequestAborted
            ),
            _ => WebhookSignatureVerificationResult.MULTIPLE_HEADERS,
        };

        if (result == WebhookSignatureVerificationResult.VALID)
        {
            await next(context);

            return;
        }

        this._logger.WebhookSignatureRejected(
            path: LogValueSanitiser.Sanitise(request.Path.Value),
            reason: result,
            delivery: LogValueSanitiser.Sanitise(request.Headers[DELIVERY_HEADER].ToString()),
            gitHubEvent: LogValueSanitiser.Sanitise(request.Headers[EVENT_HEADER].ToString()),
            remoteIpAddress: this.GetRemoteIpAddress(context),
            forwardedFor: LogValueSanitiser.Sanitise(
                request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName].ToString()
            )
        );

        context.Response.StatusCode = result switch
        {
            WebhookSignatureVerificationResult.BODY_TOO_LARGE => StatusCodes.Status413PayloadTooLarge,
            WebhookSignatureVerificationResult.BODY_READ_FAILED => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status401Unauthorized,
        };
    }

    private string GetRemoteIpAddress(HttpContext context)
    {
        try
        {
            return this._remoteIpAddressRetriever.Get(context).ToString();
        }
        catch (ArgumentException)
        {
            // The retriever throws when the transport has no remote address (Unix socket, TestServer), and a rejection must still be logged.
            return string.Empty;
        }
    }

    private async ValueTask<WebhookSignatureVerificationResult> VerifyBodyAsync(
        HttpRequest request,
        string signatureHeader,
        CancellationToken cancellationToken
    )
    {
        // GitHub always sends Content-Length, so an oversized delivery is rejected before any of it is read.
        if (request.ContentLength > BUFFER_LIMIT)
        {
            return WebhookSignatureVerificationResult.BODY_TOO_LARGE;
        }

        // Counts what the buffering stream pulls from the transport, so a read failure can be told apart from the limit.
        ReadCountingStream transport = new(request.Body);
        request.Body = transport;

        // Model binding reads the body after this, so it must be buffered to be read twice.
        request.EnableBuffering(bufferThreshold: BUFFER_THRESHOLD, bufferLimit: BUFFER_LIMIT);

        // EnableBuffering replaces an unseekable body, so read it only after the call.
        Stream body = request.Body;

        try
        {
            WebhookSignatureVerificationResult result = await this._verifier.VerifyAsync(
                body: body,
                signatureHeader: signatureHeader,
                cancellationToken: cancellationToken
            );

            body.Position = 0;

            return result;
        }
        catch (IOException) when (!cancellationToken.IsCancellationRequested)
        {
            // The buffering stream only throws for the limit after pulling a chunk that takes the total past it; a truncated
            // or malformed body fails inside the transport read, before the count can pass the limit.
            return transport.BytesRead > BUFFER_LIMIT
                ? WebhookSignatureVerificationResult.BODY_TOO_LARGE
                : WebhookSignatureVerificationResult.BODY_READ_FAILED;
        }
    }
}
