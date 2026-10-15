using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
    private static readonly DateTimeOffset Now = new(
        year: 2026,
        month: 1,
        day: 2,
        hour: 3,
        minute: 4,
        second: 5,
        offset: TimeSpan.Zero
    );

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
    [InlineData(HttpStatusCode.TooManyRequests, 3600, null, 60)]
    [InlineData(HttpStatusCode.TooManyRequests, null, 5, 5)]
    [InlineData(HttpStatusCode.TooManyRequests, null, 120, 60)]
    [InlineData(HttpStatusCode.TooManyRequests, null, -5, 0)]
    [InlineData(HttpStatusCode.TooManyRequests, null, null, 2)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 5, null, 2)]
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

        ServiceCollection services = new();
        services
            .AddLogging()
            .AddDiscord(Config)
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDiscordWebhookClient client = serviceProvider.GetRequiredService<IDiscordWebhookClient>();

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: EmbedMapper.ToPayload(TestEmbeds.WithTitle("Title")),
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: new(Success: true, StatusCode: HttpStatusCode.NoContent), actual: result);
        Assert.Equal(expected: 2, actual: handler.Requests.Count);
        Assert.All(
            collection: handler.Requests,
            action: static r =>
                Assert.Equal(
                    expected: """{"embeds":[{"title":"Title","author":{"name":"FunFair BuildBot","url":"https://funfair.io","icon_url":"https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png"},"fields":[]}]}""",
                    actual: r.Body
                )
        );
    }

    private static HttpMessageHandler CreateNoContentHandler()
    {
        return new TestHttpMessageHandler(static () => new HttpResponseMessage(HttpStatusCode.NoContent));
    }
}
