using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Filters.LoggingExtensions;
using FunFair.Common.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BuildBot.GitHub.Filters;

public sealed class GitHubWebhookSignatureFilter : IEndpointFilter
{
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";
    private const string DELIVERY_HEADER = "X-GitHub-Delivery";
    private const string EVENT_HEADER = "X-GitHub-Event";
    private const string FORWARDED_FOR_HEADER = "X-Forwarded-For";

    private readonly ILogger<GitHubWebhookSignatureFilter> _logger;
    private readonly IRemoteIpAddressRetriever _remoteIpAddressRetriever;
    private readonly IWebhookSignatureVerifier _verifier;

    public GitHubWebhookSignatureFilter(
        IWebhookSignatureVerifier verifier,
        IRemoteIpAddressRetriever remoteIpAddressRetriever,
        ILogger<GitHubWebhookSignatureFilter> logger
    )
    {
        this._verifier = verifier;
        this._remoteIpAddressRetriever = remoteIpAddressRetriever;
        this._logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!this._verifier.IsEnabled)
        {
            return await next(context);
        }

        HttpRequest request = context.HttpContext.Request;
        byte[] body = await ReadBodyAsync(request: request, cancellationToken: context.HttpContext.RequestAborted);

        WebhookSignatureVerificationResult result = this.Verify(
            body: body,
            signatures: request.Headers[SIGNATURE_HEADER]
        );

        if (result == WebhookSignatureVerificationResult.VALID)
        {
            return await next(context);
        }

        this._logger.WebhookSignatureRejected(
            path: request.Path.Value ?? string.Empty,
            reason: result,
            delivery: request.Headers[DELIVERY_HEADER].ToString(),
            gitHubEvent: request.Headers[EVENT_HEADER].ToString(),
            remoteIpAddress: this._remoteIpAddressRetriever.Get(context.HttpContext).ToString(),
            forwardedFor: request.Headers[FORWARDED_FOR_HEADER].ToString()
        );

        return Results.Unauthorized();
    }

    private WebhookSignatureVerificationResult Verify(byte[] body, in StringValues signatures)
    {
        return signatures.Count switch
        {
            0 => this._verifier.Verify(body: body, signatureHeader: null),
            1 => this._verifier.Verify(body: body, signatureHeader: signatures[0]),
            _ => WebhookSignatureVerificationResult.MULTIPLE_HEADERS,
        };
    }

    private static async ValueTask<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        // Model binding has already read the body, so this needs buffering enabled before the endpoint runs.
        Stream body = request.Body;
        body.Position = 0;

        await using (MemoryStream buffer = new())
        {
            await body.CopyToAsync(destination: buffer, cancellationToken: cancellationToken);
            body.Position = 0;

            return buffer.ToArray();
        }
    }
}
