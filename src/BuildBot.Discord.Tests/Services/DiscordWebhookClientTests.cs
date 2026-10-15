using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.Logging;
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

    private (
        DiscordWebhookClient Client,
        ILogger<DiscordWebhookClient> Logger,
        FakeTimeProvider TimeProvider
    ) CreateClient(TestHttpMessageHandler handler)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory
            .CreateClient(DiscordWebhookClient.HttpClientName)
            .Returns(_ => new HttpClient(handler: handler, disposeHandler: false));

        ILogger<DiscordWebhookClient> logger = this.GetTypedLogger<DiscordWebhookClient>();
        FakeTimeProvider timeProvider = MockDateTimeSources.Past;

        return (new(httpClientFactory: factory, timeProvider: timeProvider, logger: logger), logger, timeProvider);
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode statusCode)
    {
        return () => new(statusCode);
    }

    private static Func<HttpResponseMessage> RateLimited(
        string body,
        TimeSpan? retryAfter = null,
        string mediaType = "application/json"
    )
    {
        return () =>
        {
            HttpResponseMessage response = new(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(content: body, encoding: Encoding.UTF8, mediaType: mediaType),
            };

            if (retryAfter is not null)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
            }

            return response;
        };
    }

    private static void AdvancePastRateLimitDelay(
        FakeTimeProvider timeProvider,
        TestHttpMessageHandler handler,
        Task<WebhookSendResult> sending,
        in TimeSpan expectedDelay
    )
    {
        int requestsBeforeDelay = handler.Requests.Count;

        timeProvider.Advance(expectedDelay - TimeSpan.FromTicks(1));
        Assert.False(condition: sending.IsCompleted, userMessage: "Retried before the rate limit delay elapsed");
        Assert.Equal(expected: requestsBeforeDelay, actual: handler.Requests.Count);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        Assert.Equal(expected: requestsBeforeDelay + 1, actual: handler.Requests.Count);
    }

    [Fact]
    public async Task SendAsync_PostsPayloadAsJsonToWebhookUrl()
    {
        using TestHttpMessageHandler handler = new(Respond(HttpStatusCode.NoContent));
        (DiscordWebhookClient client, _, _) = this.CreateClient(handler);

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: new(Success: true, StatusCode: HttpStatusCode.NoContent), actual: result);

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
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task SendAsync_ReturnsResultForStatusWithoutThrowingAsync(
        HttpStatusCode statusCode,
        bool expectedSuccess
    )
    {
        using TestHttpMessageHandler handler = new(Respond(statusCode));
        (DiscordWebhookClient client, _, _) = this.CreateClient(handler);

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: new(Success: expectedSuccess, StatusCode: statusCode), actual: result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_WhenRateLimited_RetriesAndSucceeds()
    {
        using TestHttpMessageHandler handler = new(
            RateLimited("""{"message":"You are being rate limited.","retry_after":1.5,"global":false}"""),
            Respond(HttpStatusCode.NoContent)
        );
        (DiscordWebhookClient client, ILogger<DiscordWebhookClient> logger, FakeTimeProvider timeProvider) =
            this.CreateClient(handler);
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Task<WebhookSendResult> sending = client
            .SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: this.CancellationToken())
            .AsTask();

        Assert.Single(handler.Requests);
        AdvancePastRateLimitDelay(
            timeProvider: timeProvider,
            handler: handler,
            sending: sending,
            expectedDelay: TimeSpan.FromSeconds(1.5)
        );

        WebhookSendResult result = await sending;

        Assert.Equal(expected: new(Success: true, StatusCode: HttpStatusCode.NoContent), actual: result);
        Assert.Equal(expected: 2, actual: handler.Requests.Count);
        Assert.All(collection: handler.Requests, action: r => Assert.Equal(expected: EXPECTED_JSON, actual: r.Body));
        Assert.Contains(
            expectedSubstring: "retrying in 1.5 seconds",
            actualString: logger.LoggedText(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task SendAsync_WhenRateLimitedWithNonJsonBody_UsesHeaderAndRetries()
    {
        using TestHttpMessageHandler handler = new(
            RateLimited(body: "<html>rate limited</html>", retryAfter: TimeSpan.FromSeconds(2), mediaType: "text/html"),
            Respond(HttpStatusCode.NoContent)
        );
        (DiscordWebhookClient client, ILogger<DiscordWebhookClient> logger, FakeTimeProvider timeProvider) =
            this.CreateClient(handler);
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Task<WebhookSendResult> sending = client
            .SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: this.CancellationToken())
            .AsTask();

        Assert.Single(handler.Requests);
        AdvancePastRateLimitDelay(
            timeProvider: timeProvider,
            handler: handler,
            sending: sending,
            expectedDelay: TimeSpan.FromSeconds(2)
        );

        WebhookSendResult result = await sending;

        Assert.Equal(expected: new(Success: true, StatusCode: HttpStatusCode.NoContent), actual: result);
        Assert.Equal(expected: 2, actual: handler.Requests.Count);
        Assert.Contains(
            expectedSubstring: "retrying in 2 seconds",
            actualString: logger.LoggedText(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task SendAsync_WhenRateLimitedOnEveryAttempt_GivesUpAfterMaximumAttempts()
    {
        const string body = """{"retry_after":5}""";
        using TestHttpMessageHandler handler = new(
            RateLimited(body),
            RateLimited(body),
            RateLimited(body),
            Respond(HttpStatusCode.NoContent)
        );
        (DiscordWebhookClient client, _, FakeTimeProvider timeProvider) = this.CreateClient(handler);
        TimeSpan expectedDelay = TimeSpan.FromSeconds(5);

        Task<WebhookSendResult> sending = client
            .SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: this.CancellationToken())
            .AsTask();

        Assert.Single(handler.Requests);
        AdvancePastRateLimitDelay(
            timeProvider: timeProvider,
            handler: handler,
            sending: sending,
            expectedDelay: expectedDelay
        );
        AdvancePastRateLimitDelay(
            timeProvider: timeProvider,
            handler: handler,
            sending: sending,
            expectedDelay: expectedDelay
        );

        WebhookSendResult result = await sending;

        Assert.Equal(expected: new(Success: false, StatusCode: HttpStatusCode.TooManyRequests), actual: result);
        Assert.Equal(expected: 3, actual: handler.Requests.Count);
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_Throws()
    {
        using TestHttpMessageHandler handler = new(Respond(HttpStatusCode.NoContent));
        (DiscordWebhookClient client, _, _) = this.CreateClient(handler);

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: cts.Token)
        );
    }

    public static TheoryData<double?, TimeSpan?, double> RateLimitDelays =>
        new()
        {
            { 2.5, null, 2.5 },
            { 2.5, TimeSpan.FromSeconds(10), 2.5 },
            { 0.0, TimeSpan.FromSeconds(10), 10 },
            { -1.0, TimeSpan.FromSeconds(10), 10 },
            { null, TimeSpan.FromSeconds(10), 10 },
            { null, TimeSpan.Zero, 1 },
            { null, null, 1 },
            { 1e300, null, 60 },
            { null, TimeSpan.FromHours(1), 60 },
        };

    [Theory]
    [MemberData(nameof(RateLimitDelays))]
    public static void CalculateRateLimitDelay_ReturnsExpectedDelay(
        double? retryAfterSeconds,
        TimeSpan? retryAfterHeader,
        double expectedSeconds
    )
    {
        TimeSpan actual = DiscordWebhookClient.CalculateRateLimitDelay(
            retryAfterSeconds: retryAfterSeconds,
            retryAfterHeader: retryAfterHeader
        );

        Assert.Equal(expected: TimeSpan.FromSeconds(expectedSeconds), actual: actual);
    }
}
