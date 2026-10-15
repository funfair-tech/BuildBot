using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Filters.LoggingExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace BuildBot.GitHub.Filters;

public sealed class GitHubWebhookSignatureFilter : IEndpointFilter
{
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";

    private readonly ILogger<GitHubWebhookSignatureFilter> _logger;
    private readonly IWebhookSignatureVerifier _verifier;

    public GitHubWebhookSignatureFilter(
        IWebhookSignatureVerifier verifier,
        ILogger<GitHubWebhookSignatureFilter> logger
    )
    {
        this._verifier = verifier;
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

        string? signature = GetSingleHeaderValue(request.Headers[SIGNATURE_HEADER]);

        if (this._verifier.IsValid(body: body, signatureHeader: signature))
        {
            return await next(context);
        }

        this._logger.WebhookSignatureRejected(path: request.Path.Value ?? string.Empty);

        return Results.Unauthorized();
    }

    private static string? GetSingleHeaderValue(in StringValues values)
    {
        return values.Count == 1 ? values[0] : null;
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
