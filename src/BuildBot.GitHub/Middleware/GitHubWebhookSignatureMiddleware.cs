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

    private readonly bool _isEnabled;
    private readonly ILogger<GitHubWebhookSignatureMiddleware> _logger;
    private readonly IRemoteIpAddressRetriever _remoteIpAddressRetriever;
    private readonly IWebhookSignatureVerifier _verifier;

    public GitHubWebhookSignatureMiddleware(
        GitHubConfiguration configuration,
        IWebhookSignatureVerifier verifier,
        IRemoteIpAddressRetriever remoteIpAddressRetriever,
        ILogger<GitHubWebhookSignatureMiddleware> logger
    )
    {
        this._isEnabled = configuration.HasWebhookSecret;
        this._verifier = verifier;
        this._remoteIpAddressRetriever = remoteIpAddressRetriever;
        this._logger = logger;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Runs for every request, so only endpoints the routes mark as GitHub webhooks pay for verification.
        if (!this._isEnabled || context.GetEndpoint()?.Metadata.GetMetadata<GitHubWebhookEndpointMetadata>() is null)
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
            path: request.Path.Value ?? string.Empty,
            reason: result,
            delivery: request.Headers[DELIVERY_HEADER].ToString(),
            gitHubEvent: request.Headers[EVENT_HEADER].ToString(),
            remoteIpAddress: this._remoteIpAddressRetriever.Get(context).ToString(),
            forwardedFor: request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName].ToString()
        );

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    private async ValueTask<WebhookSignatureVerificationResult> VerifyBodyAsync(
        HttpRequest request,
        string signatureHeader,
        CancellationToken cancellationToken
    )
    {
        // Model binding reads the body after this, so it must be buffered to be read twice.
        request.EnableBuffering();

        // EnableBuffering replaces an unseekable body, so read it only after the call.
        Stream body = request.Body;

        WebhookSignatureVerificationResult result = await this._verifier.VerifyAsync(
            body: body,
            signatureHeader: signatureHeader,
            cancellationToken: cancellationToken
        );

        body.Position = 0;

        return result;
    }
}
