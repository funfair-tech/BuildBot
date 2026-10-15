using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Filters;
using BuildBot.GitHub.Helpers;
using BuildBot.GitHub.Models;
using BuildBot.ServiceModel.GitHub;
using FunFair.Common.Web.Interfaces;
using FunFair.Test.Common;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace BuildBot.GitHub.Tests.Filters;

public sealed class GitHubWebhookSignatureFilterTests : TestBase
{
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";

    private const string REMOTE_IP_ADDRESS = "10.0.0.5";

    private readonly ILogger<GitHubWebhookSignatureFilter> _logger;
    private readonly IMediator _mediator;
    private readonly IRemoteIpAddressRetriever _remoteIpAddressRetriever;

    public GitHubWebhookSignatureFilterTests()
    {
        this._mediator = GetSubstitute<IMediator>();
        this._remoteIpAddressRetriever = GetSubstitute<IRemoteIpAddressRetriever>();
        this._remoteIpAddressRetriever.Get(Arg.Any<HttpContext>()).Returns(IPAddress.Parse(REMOTE_IP_ADDRESS));
        this._logger = this.GetTypedLogger<GitHubWebhookSignatureFilter>();
        this._logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public async Task ValidSignaturePublishesAndReturnsNoContentAsync()
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(
            body: body,
            signature: GitHubWebhookTestVectors.ValidSignature
        );

        object? result = await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        Assert.IsType<NoContent>(result);
        await this.AssertPublishedAsync(1);
        Assert.Equal(expected: 0, actual: body.Position);
    }

    [Theory]
    [InlineData(null, WebhookSignatureVerificationResult.MISSING_HEADER)]
    [InlineData("sha256=not-a-signature", WebhookSignatureVerificationResult.MALFORMED_HEADER)]
    [InlineData(GitHubWebhookTestVectors.MismatchedSignature, WebhookSignatureVerificationResult.SIGNATURE_MISMATCH)]
    public async Task InvalidSignatureReturnsUnauthorisedWithoutPublishingAsync(
        string? signature,
        WebhookSignatureVerificationResult expectedReason
    )
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: signature);

        object? result = await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        Assert.IsType<UnauthorizedHttpResult>(result);
        await this.AssertPublishedAsync(0);
        this.AssertRejectionLogged(
            new(
                Path: string.Empty,
                Reason: expectedReason,
                Delivery: string.Empty,
                GitHubEvent: string.Empty,
                RemoteIpAddress: REMOTE_IP_ADDRESS,
                ForwardedFor: string.Empty
            )
        );
    }

    [Fact]
    public async Task MultipleSignatureHeadersReturnUnauthorisedWithoutPublishingAsync()
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: null);
        httpContext.Request.Headers[SIGNATURE_HEADER] = new StringValues([
            GitHubWebhookTestVectors.ValidSignature,
            GitHubWebhookTestVectors.ValidSignature,
        ]);

        object? result = await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        Assert.IsType<UnauthorizedHttpResult>(result);
        await this.AssertPublishedAsync(0);
        this.AssertRejectionLogged(
            new(
                Path: string.Empty,
                Reason: WebhookSignatureVerificationResult.MULTIPLE_HEADERS,
                Delivery: string.Empty,
                GitHubEvent: string.Empty,
                RemoteIpAddress: REMOTE_IP_ADDRESS,
                ForwardedFor: string.Empty
            )
        );
    }

    [Fact]
    public async Task RejectionLogsRequestOriginAsync()
    {
        await using MemoryStream body = CreateConsumedBody();
        DefaultHttpContext httpContext = CreateHttpContext(
            body: body,
            signature: GitHubWebhookTestVectors.MismatchedSignature
        );
        httpContext.Request.Path = "/github";
        httpContext.Request.Headers["X-GitHub-Delivery"] = "72d3162e-cc78-11e3-81ab-4c9367dc0958";
        httpContext.Request.Headers["X-GitHub-Event"] = "push";
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 198.51.100.2";

        object? result = await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        Assert.IsType<UnauthorizedHttpResult>(result);
        this.AssertRejectionLogged(
            new(
                Path: "/github",
                Reason: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
                Delivery: "72d3162e-cc78-11e3-81ab-4c9367dc0958",
                GitHubEvent: "push",
                RemoteIpAddress: REMOTE_IP_ADDRESS,
                ForwardedFor: "203.0.113.7, 198.51.100.2"
            )
        );
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
        MemoryStream body = new(GitHubWebhookTestVectors.Payload.ToArray());

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
            remoteIpAddressRetriever: this._remoteIpAddressRetriever,
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

    private void AssertRejectionLogged(in ExpectedRejection expected)
    {
        this._logger.Received(1).IsEnabled(LogLevel.Warning);

        ICall logCall = Assert.Single(
            this._logger.ReceivedCalls(),
            predicate: static call =>
                StringComparer.Ordinal.Equals(x: call.GetMethodInfo().Name, y: nameof(ILogger.Log))
        );
        IReadOnlyList<KeyValuePair<string, object?>> state = Assert.IsAssignableFrom<
            IReadOnlyList<KeyValuePair<string, object?>>
        >(logCall.GetArguments()[2]);
        Dictionary<string, object?> values = state.ToDictionary(StringComparer.Ordinal);

        Assert.Equal(expected: expected.Path, actual: values["Path"]);
        Assert.Equal(expected: expected.Reason, actual: values["Reason"]);
        Assert.Equal(expected: expected.Delivery, actual: values["Delivery"]);
        Assert.Equal(expected: expected.GitHubEvent, actual: values["GitHubEvent"]);
        Assert.Equal(expected: expected.RemoteIpAddress, actual: values["RemoteIpAddress"]);
        Assert.Equal(expected: expected.ForwardedFor, actual: values["ForwardedFor"]);
        Assert.DoesNotContain(
            collection: values.Values,
            filter: static value =>
                value is string text && text.Contains(value: "sha256=", comparisonType: StringComparison.Ordinal)
        );
    }

    [DebuggerDisplay("{Reason} {Path} {Delivery} {GitHubEvent} {RemoteIpAddress} {ForwardedFor}")]
    private readonly record struct ExpectedRejection(
        string Path,
        WebhookSignatureVerificationResult Reason,
        string Delivery,
        string GitHubEvent,
        string RemoteIpAddress,
        string ForwardedFor
    );

    private ValueTask AssertPublishedAsync(int times)
    {
        return this._mediator.Received(times).Publish(Arg.Any<GithubPing>(), Arg.Any<CancellationToken>());
    }
}
