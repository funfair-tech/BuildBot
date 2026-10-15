using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using BuildBot.ServiceModel.ComponentStatus;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordBotTests : TestBase
{
    private const string SECRET = "secret-token";
    private static readonly Uri WebhookUrl = new("https://example.com/webhook/" + SECRET);
    private static readonly DiscordBotConfiguration Config = new(WebhookUrl);

    private static readonly DiscordEmbed Embed = new(
        Title: "Test Message",
        Url: new("https://example.com/build"),
        Description: "Description",
        Colour: DiscordColour.Green,
        Fields: [new(Name: "Name", Value: "Value")]
    );

    private static (IDiscordWebhookClient Client, CapturingLogger<DiscordBot> Logger, DiscordBot Bot) CreateBot(
        DiscordBotConfiguration configuration
    )
    {
        IDiscordWebhookClient client = GetSubstitute<IDiscordWebhookClient>();
        CapturingLogger<DiscordBot> logger = new();

        DiscordBot bot = new(client: client, botConfiguration: configuration, logger: logger);

        return (client, logger, bot);
    }

    private static void MockDiscordWebhookClientSend(IDiscordWebhookClient client, in WebhookSendResult result)
    {
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(result);
    }

    private static void AssertSecretNeverLogged(CapturingLogger<DiscordBot> logger)
    {
        Assert.DoesNotContain(
            collection: logger.Entries,
            filter: static entry => entry.Message.Contains(value: SECRET, comparisonType: StringComparison.Ordinal)
        );
    }

    private static int LogCallsAt(LogLevel level, CapturingLogger<DiscordBot> logger)
    {
        return logger.Entries.Count(entry => entry.Level == level);
    }

    [Theory]
    [InlineData("https://example.com/webhook/secret-token", true)]
    [InlineData("http://example.com/webhook/secret-token", false)]
    public static void GetStatus_ReflectsConfiguration(string webhookUrl, bool expectedOk)
    {
        (_, _, DiscordBot bot) = CreateBot(new(new Uri(webhookUrl)));

        ServiceStatus status = bot.GetStatus();

        Assert.Equal(expected: new ServiceStatus(Name: "Discord", Ok: expectedOk), actual: status);
    }

    [Fact]
    public static void GetStatus_WithRelativeUrl_IsNotOk()
    {
        (_, _, DiscordBot bot) = CreateBot(new(new Uri(uriString: "webhook", uriKind: UriKind.Relative)));

        Assert.False(condition: bot.GetStatus().Ok, userMessage: "Relative webhook URL should not be Ok");
    }

    [Fact]
    public async Task PublishAsync_SendsMappedEmbedToWebhookAndLogsSuccess()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot(Config);
        WebhookPayload? captured = null;
        client
            .SendAsync(
                webhookUrl: WebhookUrl,
                payload: Arg.Do<WebhookPayload>(p => captured = p),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(new WebhookSendResult(Success: true, StatusCode: HttpStatusCode.NoContent));

        await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken());

        WebhookPayload payload = AssertReallyNotNull(captured);
        WebhookEmbed sent = Assert.Single(payload.Embeds);
        WebhookEmbed expected = EmbedMapper.ToWebhookEmbed(Embed);
        Assert.Equal(expected: expected with { Fields = sent.Fields }, actual: sent);
        Assert.Equal(expected: expected.Fields, actual: sent.Fields);
        Assert.Equal(expected: 2, actual: LogCallsAt(level: LogLevel.Information, logger: logger));
        Assert.Equal(expected: 0, actual: LogCallsAt(level: LogLevel.Error, logger: logger));
        AssertSecretNeverLogged(logger);
    }

    [Fact]
    public async Task PublishAsync_WhenWebhookRejectsMessage_LogsError()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot(Config);
        MockDiscordWebhookClientSend(
            client: client,
            result: new(Success: false, StatusCode: HttpStatusCode.BadRequest)
        );

        await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken());

        Assert.Equal(expected: 1, actual: LogCallsAt(level: LogLevel.Error, logger: logger));
        AssertSecretNeverLogged(logger);
    }

    [Fact]
    public async Task PublishAsync_WhenClientThrows_LogsErrorAndDoesNotThrow()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot(Config);
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("connection failed"));

        await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken());

        Assert.Equal(expected: 1, actual: LogCallsAt(level: LogLevel.Error, logger: logger));
        AssertSecretNeverLogged(logger);
    }

    [Fact]
    public async Task PublishAsync_WhenCancelled_LogsWarningAndDoesNotThrow()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot(Config);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        await bot.PublishAsync(embed: Embed, cancellationToken: cts.Token);

        Assert.Equal(expected: 1, actual: LogCallsAt(level: LogLevel.Warning, logger: logger));
        Assert.Equal(expected: 0, actual: LogCallsAt(level: LogLevel.Error, logger: logger));
    }

    [Fact]
    public async Task PublishAsync_WhenClientCancelsWithoutCallerCancellation_LogsError()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot(Config);
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException("timed out"));

        await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken());

        Assert.Equal(expected: 1, actual: LogCallsAt(level: LogLevel.Error, logger: logger));
    }

    [Fact]
    public async Task PublishAsync_WhenNotConfigured_LogsErrorWithoutSending()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot(
            new(new Uri("http://example.com/webhook/" + SECRET))
        );

        await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken());

        await client
            .DidNotReceive()
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        Assert.Equal(expected: 1, actual: LogCallsAt(level: LogLevel.Error, logger: logger));
        AssertSecretNeverLogged(logger);
    }
}
