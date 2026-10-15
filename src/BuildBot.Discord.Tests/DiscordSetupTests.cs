using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    private static readonly Uri WebhookUrl = new("https://example.com/webhook/" + SECRET);

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

    [Fact]
    public async Task WebhookHttpClient_DoesNotLogRequestUrl()
    {
        CapturingLogger<DiscordSetupTests> httpLogger = new();

        ILoggerProvider loggerProvider = GetSubstitute<ILoggerProvider>();
        loggerProvider.CreateLogger(Arg.Any<string>()).Returns(httpLogger);

        ServiceCollection services = new();
        services
            .AddLogging(builder => builder.AddProvider(loggerProvider))
            .AddDiscord(new(WebhookUrl))
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
    public async Task WebhookClient_RetriesTransientServerErrorsWithSamePayload()
    {
        using TestHttpMessageHandler handler = new(
            static () => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            static () => new HttpResponseMessage(HttpStatusCode.NoContent)
        );

        ServiceCollection services = new();
        services
            .AddLogging()
            .AddDiscord(new(WebhookUrl))
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
