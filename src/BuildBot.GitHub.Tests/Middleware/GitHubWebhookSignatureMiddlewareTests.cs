using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Helpers;
using BuildBot.GitHub.Middleware;
using FunFair.Common.Web.Interfaces;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace BuildBot.GitHub.Tests.Middleware;

public sealed class GitHubWebhookSignatureMiddlewareTests : TestBase
{
    private const string SIGNATURE_HEADER = "X-Hub-Signature-256";

    private const string REMOTE_IP_ADDRESS = "10.0.0.5";

    private const long GITHUB_PAYLOAD_LIMIT = 25L * 1024 * 1024;

    private readonly ILogger<GitHubWebhookSignatureMiddleware> _logger;
    private readonly IRemoteIpAddressRetriever _remoteIpAddressRetriever;

    private readonly List<byte[]> _bodiesSeenByNext = [];

    public GitHubWebhookSignatureMiddlewareTests()
    {
        this._remoteIpAddressRetriever = GetSubstitute<IRemoteIpAddressRetriever>();
        this._remoteIpAddressRetriever.Get(Arg.Any<HttpContext>()).Returns(IPAddress.Parse(REMOTE_IP_ADDRESS));
        this._logger = this.GetTypedLogger<GitHubWebhookSignatureMiddleware>();
        this._logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public async Task ValidSignatureCallsNextWithTheWholeBodyAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(
            body: CreateBody(),
            signature: GitHubWebhookTestVectors.ValidSignature
        );

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertNextReceivedPayload();
        Assert.Equal(expected: StatusCodes.Status200OK, actual: httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ValidSignatureWithUnseekableBodyCallsNextWithTheWholeBodyAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(
            body: new UnseekableReadStream(CreateBody()),
            signature: GitHubWebhookTestVectors.ValidSignature
        );

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertNextReceivedPayload();
    }

    [Theory]
    [InlineData(null, WebhookSignatureVerificationResult.MISSING_HEADER)]
    [InlineData("sha256=not-a-signature", WebhookSignatureVerificationResult.MALFORMED_HEADER)]
    [InlineData(GitHubWebhookTestVectors.MismatchedSignature, WebhookSignatureVerificationResult.SIGNATURE_MISMATCH)]
    public async Task InvalidSignatureReturnsUnauthorisedWithoutCallingNextAsync(
        string? signature,
        WebhookSignatureVerificationResult expectedReason
    )
    {
        DefaultHttpContext httpContext = CreateWebhookContext(body: CreateBody(), signature: signature);

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertUnauthorisedWithoutCallingNext(httpContext);
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
    public async Task MultipleSignatureHeadersReturnUnauthorisedWithoutCallingNextAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(body: CreateBody(), signature: null);
        httpContext.Request.Headers[SIGNATURE_HEADER] = new StringValues([
            GitHubWebhookTestVectors.ValidSignature,
            GitHubWebhookTestVectors.ValidSignature,
        ]);

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertUnauthorisedWithoutCallingNext(httpContext);
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
    public async Task ContentLengthOverLimitReturnsPayloadTooLargeWithoutReadingBodyAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(
            body: CreateBody(),
            signature: GitHubWebhookTestVectors.ValidSignature
        );
        httpContext.Request.ContentLength = GITHUB_PAYLOAD_LIMIT + 1;

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertPayloadTooLargeWithoutCallingNext(httpContext);
    }

    [Fact]
    public async Task BodyWithoutContentLengthOverLimitReturnsPayloadTooLargeAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(
            body: new ZeroFilledReadStream(GITHUB_PAYLOAD_LIMIT + 1),
            signature: GitHubWebhookTestVectors.ValidSignature
        );

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertPayloadTooLargeWithoutCallingNext(httpContext);
    }

    [Fact]
    public async Task BodyReadFailureAfterClientDisconnectIsNotReportedAsTooLargeAsync()
    {
        using CancellationTokenSource requestAborted = new();
        DefaultHttpContext httpContext = CreateWebhookContext(
            body: new AbortingReadStream(requestAborted),
            signature: GitHubWebhookTestVectors.ValidSignature
        );
        httpContext.RequestAborted = requestAborted.Token;

        await Assert.ThrowsAsync<IOException>(() =>
            this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext)
        );

        Assert.Empty(this._bodiesSeenByNext);
    }

    [Fact]
    public async Task RejectionLogsRequestOriginAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(
            body: CreateBody(),
            signature: GitHubWebhookTestVectors.MismatchedSignature
        );
        httpContext.Request.Path = "/github";
        httpContext.Request.Headers["X-GitHub-Delivery"] = "72d3162e-cc78-11e3-81ab-4c9367dc0958";
        httpContext.Request.Headers["X-GitHub-Event"] = "push";
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 198.51.100.2";

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertUnauthorisedWithoutCallingNext(httpContext);
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
    public async Task NoSecretConfiguredCallsNextWithoutSignatureAsync()
    {
        DefaultHttpContext httpContext = CreateWebhookContext(body: CreateBody(), signature: null);

        await this.InvokeAsync(secret: string.Empty, httpContext: httpContext);

        this.AssertNextReceivedPayload();
    }

    [Fact]
    public async Task EndpointWithoutWebhookMetadataCallsNextWithoutSignatureAsync()
    {
        DefaultHttpContext httpContext = CreateHttpContext(body: CreateBody(), signature: null);
        httpContext.SetEndpoint(
            new Endpoint(requestDelegate: null, metadata: EndpointMetadataCollection.Empty, displayName: "other")
        );

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertNextReceivedPayload();
    }

    [Fact]
    public async Task RequestWithoutEndpointCallsNextWithoutSignatureAsync()
    {
        DefaultHttpContext httpContext = CreateHttpContext(body: CreateBody(), signature: null);

        await this.InvokeAsync(secret: GitHubWebhookTestVectors.Secret, httpContext: httpContext);

        this.AssertNextReceivedPayload();
    }

    private static MemoryStream CreateBody()
    {
        return new(GitHubWebhookTestVectors.Payload.ToArray());
    }

    private static DefaultHttpContext CreateWebhookContext(Stream body, string? signature)
    {
        DefaultHttpContext httpContext = CreateHttpContext(body: body, signature: signature);
        httpContext.SetEndpoint(
            new Endpoint(
                requestDelegate: null,
                metadata: new EndpointMetadataCollection(new GitHubWebhookEndpointMetadata()),
                displayName: "webhook"
            )
        );

        return httpContext;
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

    private Task InvokeAsync(string secret, HttpContext httpContext)
    {
        GitHubConfiguration configuration = new(secret);
        GitHubWebhookSignatureMiddleware middleware = new(
            configuration: configuration,
            verifier: new WebhookSignatureVerifier(configuration),
            remoteIpAddressRetriever: this._remoteIpAddressRetriever,
            logger: this._logger
        );

        return middleware.InvokeAsync(context: httpContext, next: this.ReadBodyAsync);
    }

    private async Task ReadBodyAsync(HttpContext context)
    {
        await using MemoryStream copy = new();
        await context.Request.Body.CopyToAsync(destination: copy, cancellationToken: context.RequestAborted);
        this._bodiesSeenByNext.Add(copy.ToArray());
    }

    private void AssertNextReceivedPayload()
    {
        byte[] bodySeenByNext = Assert.Single(this._bodiesSeenByNext);
        Assert.Equal(expected: GitHubWebhookTestVectors.Payload.ToArray(), actual: bodySeenByNext);
    }

    private void AssertUnauthorisedWithoutCallingNext(HttpContext httpContext)
    {
        Assert.Equal(expected: StatusCodes.Status401Unauthorized, actual: httpContext.Response.StatusCode);
        Assert.Empty(this._bodiesSeenByNext);
    }

    private void AssertPayloadTooLargeWithoutCallingNext(HttpContext httpContext)
    {
        Assert.Equal(expected: StatusCodes.Status413PayloadTooLarge, actual: httpContext.Response.StatusCode);
        Assert.Empty(this._bodiesSeenByNext);
        this.AssertRejectionLogged(
            new(
                Path: string.Empty,
                Reason: WebhookSignatureVerificationResult.BODY_TOO_LARGE,
                Delivery: string.Empty,
                GitHubEvent: string.Empty,
                RemoteIpAddress: REMOTE_IP_ADDRESS,
                ForwardedFor: string.Empty
            )
        );
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
}
