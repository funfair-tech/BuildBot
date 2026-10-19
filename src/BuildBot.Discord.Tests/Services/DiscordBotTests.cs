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
    private const string REJECTION_BODY = """{"embeds":["0"]}""";
    private static readonly DiscordBotConfiguration Config = DiscordBotConfiguration.FromWebhookUrl(
        "https://example.com/webhook/" + SECRET
    );

    private static readonly DiscordEmbed Embed = new(
        Title: "Test Message",
        Url: new("https://example.com/build"),
        Description: "Description",
        Color: DiscordColor.Green,
        Fields: [new(Name: "Name", Value: "Value")]
    );

    private static (IDiscordWebhookClient Client, CapturingLogger<DiscordBot> Logger, DiscordBot Bot) CreateBot()
    {
        IDiscordWebhookClient client = GetSubstitute<IDiscordWebhookClient>();
        CapturingLogger<DiscordBot> logger = new();

        DiscordBot bot = new(client: client, botConfiguration: Config, logger: logger);

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

    private static void MockDiscordWebhookClientSendThrows(IDiscordWebhookClient client, Exception exception)
    {
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(exception);
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

    [Fact]
    public static void GetStatus_IsOk()
    {
        (_, _, DiscordBot bot) = CreateBot();

        ServiceStatus status = bot.GetStatus();

        Assert.Equal(expected: new ServiceStatus(Name: "Discord", Ok: true), actual: status);
    }

    [Fact]
    public async Task PublishAsync_SendsMappedEmbedToWebhookAndLogsSuccess()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot();
        WebhookPayload? captured = null;
        client
            .SendAsync(
                webhookUrl: Config.WebhookUrl,
                payload: Arg.Do<WebhookPayload>(p => captured = p),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(
                new WebhookSendResult(Success: true, StatusCode: HttpStatusCode.NoContent, ResponseBody: string.Empty)
            );

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
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot();
        MockDiscordWebhookClientSend(
            client: client,
            result: new(Success: false, StatusCode: HttpStatusCode.BadRequest, ResponseBody: REJECTION_BODY)
        );

        await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken());

        CapturedLogEntry error = Assert.Single(
            collection: logger.Entries,
            predicate: static entry => entry.Level == LogLevel.Error
        );
        Assert.Contains(
            expectedSubstring: REJECTION_BODY,
            actualString: error.Message,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "BadRequest",
            actualString: error.Message,
            comparisonType: StringComparison.Ordinal
        );
        AssertSecretNeverLogged(logger);
    }

    [Fact]
    public async Task PublishAsync_WhenClientThrows_PropagatesTheException()
    {
        (IDiscordWebhookClient client, CapturingLogger<DiscordBot> logger, DiscordBot bot) = CreateBot();
        HttpRequestException exception = new("connection failed");
        MockDiscordWebhookClientSendThrows(client: client, exception: exception);

        HttpRequestException thrown = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await bot.PublishAsync(embed: Embed, cancellationToken: this.CancellationToken())
        );

        Assert.Same(expected: exception, actual: thrown);
        AssertSecretNeverLogged(logger);
    }
}
