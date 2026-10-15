using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Filters.LoggingExtensions;
using FunFair.Common.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BuildBot.GitHub.Filters;

public sealed class GitHubWebhookSignatureFilter : IEndpointFilter
{
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";
    private const string DELIVERY_HEADER = "X-GitHub-Delivery";
    private const string EVENT_HEADER = "X-GitHub-Event";

    private readonly bool _isEnabled;
    private readonly ILogger<GitHubWebhookSignatureFilter> _logger;
    private readonly IRemoteIpAddressRetriever _remoteIpAddressRetriever;
    private readonly IWebhookSignatureVerifier _verifier;

    public GitHubWebhookSignatureFilter(
        GitHubConfiguration configuration,
        IWebhookSignatureVerifier verifier,
        IRemoteIpAddressRetriever remoteIpAddressRetriever,
        ILogger<GitHubWebhookSignatureFilter> logger
    )
    {
        this._isEnabled = configuration.HasWebhookSecret;
        this._verifier = verifier;
        this._remoteIpAddressRetriever = remoteIpAddressRetriever;
        this._logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!this._isEnabled)
        {
            return await next(context);
        }

        HttpRequest request = context.HttpContext.Request;
        StringValues signatures = request.Headers[SIGNATURE_HEADER];

        // Only read the body once there is a single signature to check it against.
        WebhookSignatureVerificationResult result = signatures.Count switch
        {
            0 => WebhookSignatureVerificationResult.MISSING_HEADER,
            1 => await this.VerifyBodyAsync(
                request: request,
                signatureHeader: signatures.ToString(),
                cancellationToken: context.HttpContext.RequestAborted
            ),
            _ => WebhookSignatureVerificationResult.MULTIPLE_HEADERS,
        };

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
            forwardedFor: request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName].ToString()
        );

        return Results.Unauthorized();
    }

    private async ValueTask<WebhookSignatureVerificationResult> VerifyBodyAsync(
        HttpRequest request,
        string signatureHeader,
        CancellationToken cancellationToken
    )
    {
        // Model binding has already read the body, so this needs buffering enabled before the endpoint runs.
        Stream body = request.Body;
        body.Position = 0;

        WebhookSignatureVerificationResult result = await this._verifier.VerifyAsync(
            body: body,
            signatureHeader: signatureHeader,
            cancellationToken: cancellationToken
        );

        body.Position = 0;

        return result;
    }
}
