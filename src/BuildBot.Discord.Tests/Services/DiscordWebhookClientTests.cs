using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordWebhookClientTests : TestBase
{
    private static readonly Uri WebhookUrl = new("https://example.com/webhook/secret-token");

    private static readonly WebhookPayload Payload = new([
        new WebhookEmbed(
            Title: "Title",
            Url: new("https://example.com/build"),
            Description: null,
            Color: 0x2ECC71,
            Author: new(Name: "Author", Url: new("https://example.com/"), IconUrl: new("https://example.com/icon.png")),
            Fields: [new(Name: "Name", Value: "Value", Inline: true)]
        ),
    ]);

    private const string EXPECTED_JSON =
        """{"embeds":[{"title":"Title","url":"https://example.com/build","color":3066993,"author":{"name":"Author","url":"https://example.com/","icon_url":"https://example.com/icon.png"},"fields":[{"name":"Name","value":"Value","inline":true}]}]}""";

    private static DiscordWebhookClient CreateClient(TestHttpMessageHandler handler)
    {
        return CreateClient(handler: handler, timeProvider: MockDateTimeSources.Past);
    }

    private static DiscordWebhookClient CreateClient(TestHttpMessageHandler handler, TimeProvider timeProvider)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory
            .CreateClient(DiscordWebhookClient.HttpClientName)
            .Returns(_ => new HttpClient(handler: handler, disposeHandler: false));

        return new(httpClientFactory: factory, timeProvider: timeProvider);
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode statusCode)
    {
        return () => new(statusCode);
    }

    [Fact]
    public async Task SendAsync_PostsPayloadAsJsonToWebhookUrl()
    {
        using TestHttpMessageHandler handler = new(Respond(HttpStatusCode.NoContent));
        DiscordWebhookClient client = CreateClient(handler);

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(
            expected: new(Success: true, StatusCode: HttpStatusCode.NoContent, ResponseBody: string.Empty),
            actual: result
        );

        TestHttpMessageHandler.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(expected: HttpMethod.Post, actual: request.Method);
        Assert.Equal(expected: WebhookUrl, actual: request.RequestUri);
        Assert.Equal(expected: "application/json", actual: request.ContentType);
        Assert.Equal(expected: EXPECTED_JSON, actual: request.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.NoContent, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task SendAsync_ReturnsResultForStatusWithoutThrowingAsync(
        HttpStatusCode statusCode,
        bool expectedSuccess
    )
    {
        using TestHttpMessageHandler handler = new(Respond(statusCode));
        DiscordWebhookClient client = CreateClient(handler);

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(
            expected: new(Success: expectedSuccess, StatusCode: statusCode, ResponseBody: string.Empty),
            actual: result
        );
        Assert.Single(handler.Requests);
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode statusCode, HttpContent content)
    {
        return () => new(statusCode) { Content = content };
    }

    private ValueTask<WebhookSendResult> SendAsync(TestHttpMessageHandler handler)
    {
        DiscordWebhookClient client = CreateClient(handler);

        return client.SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: this.CancellationToken());
    }

    [Fact]
    public async Task SendAsync_WhenRejected_ReturnsResponseBody()
    {
        const string body =
            """{"message":"Invalid Form Body","errors":{"embeds":{"0":{"title":"Must be 256 or fewer"}}}}""";
        using StringContent content = new(body);
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));

        WebhookSendResult result = await this.SendAsync(handler);

        Assert.Equal(
            expected: new(Success: false, StatusCode: HttpStatusCode.BadRequest, ResponseBody: body),
            actual: result
        );
    }

    [Fact]
    public async Task SendAsync_WhenRejectedWithLongBody_TruncatesResponseBody()
    {
        string body = new string(c: 'a', count: DiscordWebhookClient.MaximumResponseBodyLength) + "TRUNCATED";
        using StringContent content = new(body);
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));

        WebhookSendResult result = await this.SendAsync(handler);

        Assert.Equal(
            expected: new(
                Success: false,
                StatusCode: HttpStatusCode.BadRequest,
                ResponseBody: body[..DiscordWebhookClient.MaximumResponseBodyLength]
            ),
            actual: result
        );
    }

    [Fact]
    public async Task SendAsync_WhenRejectedWithHugeBody_DoesNotBufferTheWholeBody()
    {
        const long bodyLength = 16 * 1024 * 1024;
        CountingReadStream stream = new(bodyLength);
        using StreamContent content = new(stream);
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));

        WebhookSendResult result = await this.SendAsync(handler);

        Assert.Equal(expected: DiscordWebhookClient.MaximumResponseBodyLength, actual: result.ResponseBody.Length);
        Assert.InRange(actual: stream.BytesRead, low: DiscordWebhookClient.MaximumResponseBodyLength, high: 64 * 1024);
    }

    [Fact]
    public async Task SendAsync_WhenSuccessful_DoesNotReadResponseBody()
    {
        using StreamContent content = new(new ThrowingReadStream(new InvalidOperationException("Body read")));
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.OK, content: content));

        WebhookSendResult result = await this.SendAsync(handler);

        Assert.Equal(
            expected: new(Success: true, StatusCode: HttpStatusCode.OK, ResponseBody: string.Empty),
            actual: result
        );
    }

    public static TheoryData<Exception> UnreadableBodyExceptions =>
        [new IOException("Connection reset"), new HttpRequestException("Response ended prematurely")];

    [Theory]
    [MemberData(nameof(UnreadableBodyExceptions))]
    public async Task SendAsync_WhenRejectedBodyCannotBeRead_StillReturnsStatusAsync(Exception exception)
    {
        using StreamContent content = new(new ThrowingReadStream(exception));
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));

        WebhookSendResult result = await this.SendAsync(handler);

        Assert.Equal(
            expected: new(
                Success: false,
                StatusCode: HttpStatusCode.BadRequest,
                ResponseBody: DiscordWebhookClient.ResponseBodyUnavailable
            ),
            actual: result
        );
    }

    [Fact]
    public async Task SendAsync_WhenRejectedBodyReadIsCancelled_Throws()
    {
        using StreamContent content = new(new ThrowingReadStream(new OperationCanceledException()));
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await this.SendAsync(handler));
    }

    [Fact]
    public async Task SendAsync_WhenRejectedBodyReadTimesOut_StillReturnsStatus()
    {
        FakeTimeProvider timeProvider = MockDateTimeSources.Past;
        StalledReadStream stream = new();
        using StreamContent content = new(stream);
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));
        DiscordWebhookClient client = CreateClient(handler: handler, timeProvider: timeProvider);

        ValueTask<WebhookSendResult> sending = client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );
        await stream.WaitForReadStartedAsync(this.CancellationToken());
        timeProvider.Advance(DiscordWebhookClient.ResponseBodyReadTimeout);

        WebhookSendResult result = await sending;

        Assert.Equal(
            expected: new(
                Success: false,
                StatusCode: HttpStatusCode.BadRequest,
                ResponseBody: DiscordWebhookClient.ResponseBodyUnavailable
            ),
            actual: result
        );
    }

    [Fact]
    public async Task SendAsync_WhenCancelledWhileReadingRejectedBody_Throws()
    {
        StalledReadStream stream = new();
        using StreamContent content = new(stream);
        using TestHttpMessageHandler handler = new(Respond(statusCode: HttpStatusCode.BadRequest, content: content));
        DiscordWebhookClient client = CreateClient(handler);
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken());

        ValueTask<WebhookSendResult> sending = client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: cts.Token
        );
        await stream.WaitForReadStartedAsync(this.CancellationToken());
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sending);
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_Throws()
    {
        using TestHttpMessageHandler handler = new(Respond(HttpStatusCode.NoContent));
        DiscordWebhookClient client = CreateClient(handler);

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: cts.Token)
        );
    }
}
