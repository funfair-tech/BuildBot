using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Filters;
using BuildBot.GitHub.Helpers;
using BuildBot.GitHub.Models;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace BuildBot.GitHub.Tests.Filters;

public sealed class GitHubWebhookSignatureFilterTests : TestBase
{
    private const string SECRET = "It's a Secret to Everybody";
    private const string VALID_SIGNATURE = "sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17";
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";

    private readonly ILogger<GitHubWebhookSignatureFilter> _logger;
    private readonly IMediator _mediator;

    public GitHubWebhookSignatureFilterTests()
    {
        this._mediator = GetSubstitute<IMediator>();
        this._logger = this.GetTypedLogger<GitHubWebhookSignatureFilter>();
        this._logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public async Task ValidSignaturePublishesAndReturnsNoContentAsync()
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: VALID_SIGNATURE);

        object? result = await this.InvokeAsync(secret: SECRET, httpContext: httpContext);

        Assert.IsType<NoContent>(result);
        await this.AssertPublishedAsync(1);
        Assert.Equal(expected: 0, actual: body.Position);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sha256=not-a-signature")]
    [InlineData("sha256=057107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    public async Task InvalidSignatureReturnsUnauthorisedWithoutPublishingAsync(string? signature)
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: signature);

        object? result = await this.InvokeAsync(secret: SECRET, httpContext: httpContext);

        Assert.IsType<UnauthorizedHttpResult>(result);
        await this.AssertPublishedAsync(0);
        this._logger.Received(1).IsEnabled(LogLevel.Warning);
    }

    [Fact]
    public async Task MultipleSignatureHeadersReturnUnauthorisedWithoutPublishingAsync()
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: null);
        httpContext.Request.Headers[SIGNATURE_HEADER] = new StringValues([VALID_SIGNATURE, VALID_SIGNATURE]);

        object? result = await this.InvokeAsync(secret: SECRET, httpContext: httpContext);

        Assert.IsType<UnauthorizedHttpResult>(result);
        await this.AssertPublishedAsync(0);
    }

    [Fact]
    public async Task NoSecretConfiguredPublishesWithoutSignatureAsync()
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: null);

        object? result = await this.InvokeAsync(secret: string.Empty, httpContext: httpContext);

        Assert.IsType<NoContent>(result);
        await this.AssertPublishedAsync(1);
    }

    private static MemoryStream CreateConsumedBody()
    {
        MemoryStream body = new("Hello, World!"u8.ToArray());

        // Model binding leaves the buffered body at its end before the filter runs.
        body.Seek(offset: 0, loc: SeekOrigin.End);

        return body;
    }

    private static DefaultHttpContext CreateHttpContext(Stream body, string? signature)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Body = body;

        if (signature is not null)
        {
            httpContext.Request.Headers[SIGNATURE_HEADER] = signature;
        }

        return httpContext;
    }

    private ValueTask<object?> InvokeAsync(string secret, HttpContext httpContext)
    {
        GitHubWebhookSignatureFilter filter = new(
            verifier: new WebhookSignatureVerifier(new GitHubConfiguration(secret)),
            logger: this._logger
        );

        return filter.InvokeAsync(
            context: EndpointFilterInvocationContext.Create(httpContext),
            next: this.PublishAsync
        );
    }

    private async ValueTask<object?> PublishAsync(EndpointFilterInvocationContext context)
    {
        await this._mediator.Publish(
            new GithubPing(new PingModel(zen: "Design for failure.", hookId: "123456789")),
            cancellationToken: context.HttpContext.RequestAborted
        );

        return Results.NoContent();
    }

    private ValueTask AssertPublishedAsync(int times)
    {
        return this._mediator.Received(times).Publish(Arg.Any<GithubPing>(), Arg.Any<CancellationToken>());
    }
}
