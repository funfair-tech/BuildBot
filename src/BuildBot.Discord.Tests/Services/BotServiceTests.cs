using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class BotServiceTests : TestBase
{
    private static readonly TimeSpan ForwardingWait = TimeSpan.FromSeconds(3);

    private static (BotService Service, IDiscordBot Bot, MessageChannel<BotMessage> MessageChannel) CreateService()
    {
        IDiscordBot bot = GetSubstitute<IDiscordBot>();
        MessageChannel<BotMessage> messageChannel = new();

        BotService service = new(bot: bot, botMessageChannel: messageChannel);

        return (service, bot, messageChannel);
    }

    [Fact]
    public async Task StartAsync_Completes()
    {
        (BotService service, IDiscordBot bot, _) = CreateService();

        using (service)
        {
            await service.StartAsync(this.CancellationToken());

            Assert.Empty(bot.ReceivedCalls());
        }
    }

    [Fact]
    public async Task StopAsync_StopsForwardingMessages()
    {
        (BotService service, IDiscordBot bot, MessageChannel<BotMessage> messageChannel) = CreateService();

        using (service)
        {
            await service.StopAsync(this.CancellationToken());

            await messageChannel.PublishAsync(
                message: new(TestEmbeds.WithTitle("Test")),
                cancellationToken: this.CancellationToken()
            );

            await Task.Delay(delay: ForwardingWait, cancellationToken: this.CancellationToken());

            await bot.DidNotReceive()
                .PublishAsync(embed: Arg.Any<DiscordEmbed>(), cancellationToken: Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public void Dispose_DisposesSubscriptions()
    {
        (BotService service, _, _) = CreateService();

        service.Dispose();
    }

    [Fact]
    public async Task BotMessage_IsForwardedToBot_AfterDelay()
    {
        (BotService service, IDiscordBot bot, MessageChannel<BotMessage> messageChannel) = CreateService();

        using (service)
        {
            DiscordEmbed embed = TestEmbeds.WithTitle("Test");

            await messageChannel.PublishAsync(message: new(embed), cancellationToken: this.CancellationToken());

            await Task.Delay(delay: ForwardingWait, cancellationToken: this.CancellationToken());

            await bot.Received(1).PublishAsync(embed: embed, cancellationToken: Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task FailedSend_DoesNotStopLaterMessagesBeingSent()
    {
        IDiscordWebhookClient client = GetSubstitute<IDiscordWebhookClient>();
        int calls = 0;
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
                Interlocked.Increment(ref calls) == 1
                    ? throw new HttpRequestException("connection failed")
                    : new WebhookSendResult(Success: true, StatusCode: HttpStatusCode.NoContent)
            );

        DiscordBot bot = new(
            client: client,
            botConfiguration: new(new Uri("https://example.com/webhook")),
            logger: this.GetTypedLogger<DiscordBot>()
        );
        MessageChannel<BotMessage> messageChannel = new();

        using (BotService service = new(bot: bot, botMessageChannel: messageChannel))
        {
            await messageChannel.PublishAsync(
                message: new(TestEmbeds.WithTitle("First")),
                cancellationToken: this.CancellationToken()
            );
            await messageChannel.PublishAsync(
                message: new(TestEmbeds.WithTitle("Second")),
                cancellationToken: this.CancellationToken()
            );

            await Task.Delay(delay: ForwardingWait, cancellationToken: this.CancellationToken());

            await client
                .Received(2)
                .SendAsync(
                    webhookUrl: Arg.Any<Uri>(),
                    payload: Arg.Any<WebhookPayload>(),
                    cancellationToken: Arg.Any<CancellationToken>()
                );
        }
    }

    public static TheoryData<int, string> NullConstructorArguments =>
        new() { { 0, "bot" }, { 1, "botMessageChannel" } };

    [Theory]
    [MemberData(nameof(NullConstructorArguments))]
    public static void Constructor_ThrowsArgumentNullException_WhenParameterIsNull(
        int nullParameterIndex,
        string expectedParamName
    )
    {
        ConstructorInfo? ctor = typeof(BotService).GetConstructor([
            typeof(IDiscordBot),
            typeof(IMessageChannel<BotMessage>),
        ]);
        Assert.NotNull(ctor);

        object?[] args = [GetSubstitute<IDiscordBot>(), GetSubstitute<IMessageChannel<BotMessage>>()];
        args[nullParameterIndex] = null;

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(args));
        ArgumentNullException ane = Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal(expected: expectedParamName, actual: ane.ParamName);
    }
}
