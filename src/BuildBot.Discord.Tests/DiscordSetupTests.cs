using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using BuildBot.Discord.Tests.Services;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.Timeout;
using Xunit;

namespace BuildBot.Discord.Tests;

public sealed class DiscordSetupTests : TestBase
{
    private const string SECRET = "secret-token";
    private const string CONTROL_CLIENT_NAME = "Control";
    private static readonly DiscordBotConfiguration Config = DiscordBotConfiguration.FromWebhookUrl(
        "https://example.com/webhook/" + SECRET
    );
    private static readonly Uri WebhookUrl = Config.WebhookUrl;
    private static readonly DateTimeOffset Now = MockDateTimeSources.Past.GetUtcNow();

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    public static void CalculateRetryDelay_IsExponential(int attempt, int expectedSeconds)
    {
        Assert.Equal(
            expected: TimeSpan.FromSeconds(expectedSeconds),
            actual: DiscordSetup.CalculateRetryDelay(attempt)
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, 5, null, 5)]
    [InlineData(HttpStatusCode.TooManyRequests, 0, null, 0)]
    [InlineData(HttpStatusCode.TooManyRequests, -5, null, 0)]
    [InlineData(HttpStatusCode.TooManyRequests, null, 5, 5)]
    [InlineData(HttpStatusCode.TooManyRequests, null, -5, 0)]
    [InlineData(HttpStatusCode.TooManyRequests, null, null, 2)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 5, null, 5)]
    [InlineData(HttpStatusCode.InternalServerError, null, 7, 7)]
    [InlineData(HttpStatusCode.InternalServerError, null, null, 2)]
    public static void CalculateRetryDelay_ForResponse_ReturnsExpectedDelay(
        HttpStatusCode statusCode,
        int? retryAfterDeltaSeconds,
        int? retryAfterDateOffsetSeconds,
        int expectedSeconds
    )
    {
        using HttpResponseMessage response = new(statusCode);
        response.Headers.RetryAfter = CreateRetryAfter(
            deltaSeconds: retryAfterDeltaSeconds,
            dateOffsetSeconds: retryAfterDateOffsetSeconds
        );

        TimeSpan actual = DiscordSetup.CalculateRetryDelay(attempt: 1, response: response, now: Now);

        Assert.Equal(expected: TimeSpan.FromSeconds(expectedSeconds), actual: actual);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, null, null, true)]
    [InlineData(HttpStatusCode.TooManyRequests, 60, null, true)]
    [InlineData(HttpStatusCode.TooManyRequests, 61, null, false)]
    [InlineData(HttpStatusCode.TooManyRequests, -5, null, true)]
    [InlineData(HttpStatusCode.TooManyRequests, null, 60, true)]
    [InlineData(HttpStatusCode.TooManyRequests, null, 61, false)]
    [InlineData(HttpStatusCode.TooManyRequests, null, -5, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 5, null, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 3600, null, false)]
    [InlineData(HttpStatusCode.InternalServerError, null, null, true)]
    [InlineData(HttpStatusCode.RequestTimeout, null, null, true)]
    [InlineData(HttpStatusCode.BadGateway, null, null, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, null, null, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, null, null, true)]
    [InlineData(HttpStatusCode.NotImplemented, null, null, false)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, null, null, false)]
    [InlineData(HttpStatusCode.InsufficientStorage, null, null, false)]
    [InlineData(HttpStatusCode.OK, null, null, false)]
    [InlineData(HttpStatusCode.NotFound, 5, null, false)]
    public static void ShouldRetry_ReturnsExpectedDecision(
        HttpStatusCode statusCode,
        int? retryAfterDeltaSeconds,
        int? retryAfterDateOffsetSeconds,
        bool expected
    )
    {
        using HttpResponseMessage response = new(statusCode);
        response.Headers.RetryAfter = CreateRetryAfter(
            deltaSeconds: retryAfterDeltaSeconds,
            dateOffsetSeconds: retryAfterDateOffsetSeconds
        );

        Assert.Equal(expected: expected, actual: DiscordSetup.ShouldRetry(response: response, now: Now));
    }

    [Fact]
    public static void CalculateRetryDelay_WithoutResponse_UsesBackoff()
    {
        TimeSpan actual = DiscordSetup.CalculateRetryDelay(attempt: 2, response: null, now: Now);

        Assert.Equal(expected: TimeSpan.FromSeconds(4), actual: actual);
    }

    private static RetryConditionHeaderValue? CreateRetryAfter(int? deltaSeconds, int? dateOffsetSeconds)
    {
        if (deltaSeconds is not null)
        {
            return new(TimeSpan.FromSeconds(deltaSeconds.Value));
        }

        if (dateOffsetSeconds is not null)
        {
            return new(Now.AddSeconds(dateOffsetSeconds.Value));
        }

        return null;
    }

    [Fact]
    public async Task WebhookHttpClient_DoesNotLogRequestUrl()
    {
        CapturingLogger<DiscordSetupTests> httpLogger = new();

        ILoggerProvider loggerProvider = GetSubstitute<ILoggerProvider>();
        loggerProvider.CreateLogger(Arg.Any<string>()).Returns(httpLogger);

        ServiceCollection services = new();
        services
            .AddLogging(builder => builder.AddProvider(loggerProvider))
            .AddDiscord(Config)
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(CreateNoContentHandler);
        services.AddHttpClient(CONTROL_CLIENT_NAME).ConfigurePrimaryHttpMessageHandler(CreateNoContentHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        using (HttpClient control = factory.CreateClient(CONTROL_CLIENT_NAME))
        {
            using HttpResponseMessage controlResponse = await control.GetAsync(
                requestUri: WebhookUrl,
                cancellationToken: this.CancellationToken()
            );
        }

        Assert.Contains(collection: httpLogger.Entries, filter: static entry => ContainsSecret(entry));
        int controlEntryCount = httpLogger.EntryCount;

        using (HttpClient client = factory.CreateClient(DiscordWebhookClient.HttpClientName))
        {
            using HttpResponseMessage response = await client.GetAsync(
                requestUri: WebhookUrl,
                cancellationToken: this.CancellationToken()
            );
        }

        Assert.DoesNotContain(
            collection: httpLogger.Entries.Skip(controlEntryCount),
            filter: static entry => ContainsSecret(entry)
        );
    }

    private static bool ContainsSecret(in CapturedLogEntry entry)
    {
        return entry.Message.Contains(value: SECRET, comparisonType: StringComparison.Ordinal);
    }

    [Fact]
    public async Task WebhookClient_RetriesRateLimitedRequestsWithSamePayload()
    {
        using TestHttpMessageHandler handler = new(
            static () =>
                new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero) },
                },
            static () => new HttpResponseMessage(HttpStatusCode.NoContent)
        );

        CapturingLogger<DiscordSetupTests> logger = new();

        WebhookSendResult result = await this.SendViaWebhookClientAsync(handler: handler, logger: logger);

        Assert.Equal(
            expected: new(Success: true, StatusCode: HttpStatusCode.NoContent, ResponseBody: string.Empty),
            actual: result
        );
        Assert.Equal(expected: 2, actual: handler.Requests.Count);
        AssertRetryWarningLoggedWithoutSecret(
            logger: logger,
            expectedMessage: "DISCORD: Webhook attempt 1 failed with HTTP TooManyRequests; retrying in 00:00:00"
        );
        Assert.All(
            collection: handler.Requests,
            action: static r =>
                Assert.Equal(
                    expected: """{"embeds":[{"title":"Title","author":{"name":"FunFair BuildBot","url":"https://funfair.io","icon_url":"https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png"},"fields":[]}]}""",
                    actual: r.Body
                )
        );
    }

    [Fact]
    public async Task WebhookClient_RetriesServiceUnavailableHonouringRetryAfter()
    {
        using TestHttpMessageHandler handler = new(
            static () =>
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero) },
                },
            static () => new HttpResponseMessage(HttpStatusCode.NoContent)
        );

        CapturingLogger<DiscordSetupTests> logger = new();

        WebhookSendResult result = await this.SendViaWebhookClientAsync(handler: handler, logger: logger);

        Assert.Equal(
            expected: new(Success: true, StatusCode: HttpStatusCode.NoContent, ResponseBody: string.Empty),
            actual: result
        );
        Assert.Equal(expected: 2, actual: handler.Requests.Count);
        AssertRetryWarningLoggedWithoutSecret(
            logger: logger,
            expectedMessage: "DISCORD: Webhook attempt 1 failed with HTTP ServiceUnavailable; retrying in 00:00:00"
        );
    }

    private static void AssertRetryWarningLoggedWithoutSecret(
        CapturingLogger<DiscordSetupTests> logger,
        string expectedMessage
    )
    {
        CapturedLogEntry warning = Assert.Single(
            collection: logger.Entries,
            predicate: static entry => entry.Level == LogLevel.Warning
        );
        Assert.Equal(expected: expectedMessage, actual: warning.Message);
        Assert.DoesNotContain(collection: logger.Entries, filter: static entry => ContainsSecret(entry));
    }

    [Fact]
    public async Task WebhookClient_DoesNotRetryWhenRetryAfterExceedsMaximum()
    {
        using TestHttpMessageHandler handler = new(static () =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(1)) },
            }
        );

        CapturingLogger<DiscordSetupTests> logger = new();

        WebhookSendResult result = await this.SendViaWebhookClientAsync(handler: handler, logger: logger);

        Assert.Equal(
            expected: new(Success: false, StatusCode: HttpStatusCode.TooManyRequests, ResponseBody: string.Empty),
            actual: result
        );
        Assert.Single(handler.Requests);
        Assert.DoesNotContain(collection: logger.Entries, filter: static entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task WebhookHttpClient_TimeoutCoversFullRetryBudget()
    {
        ServiceCollection services = new();
        services.AddLogging().AddDiscord(Config);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using HttpClient client = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(DiscordWebhookClient.HttpClientName);

        // 3 retries waiting up to 60s each, plus 4 attempts of 30s each.
        Assert.Equal(expected: TimeSpan.FromSeconds(300), actual: client.Timeout);
        Assert.Equal(expected: DiscordSetup.WebhookTimeout, actual: client.Timeout);
    }

    [Fact]
    public async Task WebhookPolicy_RetriesTimedOutAttempt()
    {
        CapturingLogger<DiscordWebhookClient> logger = new();
        IAsyncPolicy<HttpResponseMessage> policy = CreateFastTimeoutPolicy(logger);
        int attempts = 0;

        using HttpResponseMessage response = await policy.ExecuteAsync(
            action: async cancellationToken =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    return await StallAsync(cancellationToken);
                }

                return new(HttpStatusCode.NoContent);
            },
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: HttpStatusCode.NoContent, actual: response.StatusCode);
        Assert.Equal(expected: 2, actual: attempts);
        AssertSingleRetryWarning(
            logger: logger,
            expectedMessage: "DISCORD: Webhook attempt 1 failed with TimeoutRejectedException; retrying in 00:00:00"
        );
    }

    [Fact]
    public async Task WebhookPolicy_RetriesFailedRequestWithoutLoggingItsMessage()
    {
        CapturingLogger<DiscordWebhookClient> logger = new();
        IAsyncPolicy<HttpResponseMessage> policy = CreateFastTimeoutPolicy(logger);
        int attempts = 0;

        using HttpResponseMessage response = await policy.ExecuteAsync(
            action: _ =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    throw new HttpRequestException("Failed to reach " + WebhookUrl);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            },
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: HttpStatusCode.NoContent, actual: response.StatusCode);
        AssertSingleRetryWarning(
            logger: logger,
            expectedMessage: "DISCORD: Webhook attempt 1 failed with HttpRequestException; retrying in 00:00:00"
        );
        Assert.DoesNotContain(collection: logger.Entries, filter: static entry => ContainsSecret(entry));
    }

    [Fact]
    public async Task WebhookPolicy_WhenEveryAttemptTimesOut_ThrowsAfterAllRetries()
    {
        CapturingLogger<DiscordWebhookClient> logger = new();
        IAsyncPolicy<HttpResponseMessage> policy = CreateFastTimeoutPolicy(logger);
        int attempts = 0;

        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await policy.ExecuteAsync(
                action: cancellationToken =>
                {
                    Interlocked.Increment(ref attempts);

                    return StallAsync(cancellationToken);
                },
                cancellationToken: this.CancellationToken()
            )
        );

        Assert.Equal(expected: 4, actual: attempts);
        string[] expectedMessages =
        [
            "DISCORD: Webhook attempt 1 failed with TimeoutRejectedException; retrying in 00:00:00",
            "DISCORD: Webhook attempt 2 failed with TimeoutRejectedException; retrying in 00:00:00",
            "DISCORD: Webhook attempt 3 failed with TimeoutRejectedException; retrying in 00:00:00",
        ];
        Assert.Equal(
            expected: expectedMessages,
            actual: logger.Entries.Select(static entry => entry.Message),
            comparer: StringComparer.Ordinal
        );
    }

    [Fact]
    public async Task WebhookPolicy_WhenCallerCancels_DoesNotRetry()
    {
        CapturingLogger<DiscordWebhookClient> logger = new();
        IAsyncPolicy<HttpResponseMessage> policy = DiscordSetup.CreateWebhookPolicy(
            timeProvider: MockDateTimeSources.Past,
            attemptTimeout: Timeout.InfiniteTimeSpan,
            backoff: static _ => TimeSpan.Zero,
            logger: logger
        );
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken());
        TaskCompletionSource attemptStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;

        ValueTask<HttpResponseMessage> executing = ExecutePolicyAsync(
            policy: policy,
            action: cancellationToken =>
            {
                Interlocked.Increment(ref attempts);
                attemptStarted.TrySetResult();

                return StallAsync(cancellationToken);
            },
            cancellationToken: cts.Token
        );
        await attemptStarted.Task.WaitAsync(this.CancellationToken());
        await cts.CancelAsync();

        // A TimeoutRejectedException is not an OperationCanceledException, so this also proves the caller's
        // cancellation was not reported as an attempt timeout.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await executing);

        Assert.Equal(expected: 1, actual: attempts);
        Assert.Empty(logger.Entries);
    }

    private static async ValueTask<HttpResponseMessage> ExecutePolicyAsync(
        IAsyncPolicy<HttpResponseMessage> policy,
        Func<CancellationToken, Task<HttpResponseMessage>> action,
        CancellationToken cancellationToken
    )
    {
        return await policy.ExecuteAsync(action: action, cancellationToken: cancellationToken);
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateFastTimeoutPolicy(ILogger<DiscordWebhookClient> logger)
    {
        return DiscordSetup.CreateWebhookPolicy(
            timeProvider: MockDateTimeSources.Past,
            attemptTimeout: TimeSpan.FromMilliseconds(1),
            backoff: static _ => TimeSpan.Zero,
            logger: logger
        );
    }

    private static void AssertSingleRetryWarning(CapturingLogger<DiscordWebhookClient> logger, string expectedMessage)
    {
        CapturedLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(expected: LogLevel.Warning, actual: entry.Level);
        Assert.Equal(expected: expectedMessage, actual: entry.Message);
    }

    private static async Task<HttpResponseMessage> StallAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(delay: Timeout.InfiniteTimeSpan, cancellationToken: cancellationToken);

        throw new InvalidOperationException("A stalled attempt only ends by cancellation");
    }

    private async ValueTask<WebhookSendResult> SendViaWebhookClientAsync(
        TestHttpMessageHandler handler,
        CapturingLogger<DiscordSetupTests> logger
    )
    {
        ILoggerProvider loggerProvider = GetSubstitute<ILoggerProvider>();
        loggerProvider.CreateLogger(Arg.Any<string>()).Returns(logger);

        ServiceCollection services = new();
        services
            .AddLogging(builder => builder.AddProvider(loggerProvider))
            .AddDiscord(Config)
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDiscordWebhookClient client = serviceProvider.GetRequiredService<IDiscordWebhookClient>();

        return await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: EmbedMapper.ToPayload(TestEmbeds.WithTitle("Title")),
            cancellationToken: this.CancellationToken()
        );
    }

    private static HttpMessageHandler CreateNoContentHandler()
    {
        return new TestHttpMessageHandler(static () => new HttpResponseMessage(HttpStatusCode.NoContent));
    }
}
