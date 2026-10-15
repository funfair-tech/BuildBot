using System;
using System.Collections.Generic;
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
    private static readonly TimeSpan CompletionWait = TimeSpan.FromSeconds(10);

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
            CancellationToken cancellationToken = this.CancellationToken();
            await service.StopAsync(cancellationToken);

            BotMessage message = new(TestEmbeds.WithTitle("Test"));
            await messageChannel.PublishAsync(message: message, cancellationToken: cancellationToken);

            // The stopped service no longer reads the channel, so the message is still there for us to read.
            await using IAsyncEnumerator<BotMessage> reader = messageChannel
                .ReadAllAsync(cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            Assert.True(
                await reader
                    .MoveNextAsync()
                    .AsTask()
                    .WaitAsync(timeout: CompletionWait, cancellationToken: cancellationToken),
                userMessage: "Message should remain unread in the channel"
            );
            Assert.Same(expected: message, actual: reader.Current);

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
    public async Task BotMessage_IsForwardedToBot()
    {
        (BotService service, IDiscordBot bot, MessageChannel<BotMessage> messageChannel) = CreateService();
        TaskCompletionSource published = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bot.When(async x =>
                await x.PublishAsync(embed: Arg.Any<DiscordEmbed>(), cancellationToken: Arg.Any<CancellationToken>())
            )
            .Do(_ => published.TrySetResult());

        using (service)
        {
            DiscordEmbed embed = TestEmbeds.WithTitle("Test");

            await messageChannel.PublishAsync(message: new(embed), cancellationToken: this.CancellationToken());

            await published.Task.WaitAsync(timeout: CompletionWait, cancellationToken: this.CancellationToken());

            await bot.Received(1).PublishAsync(embed: embed, cancellationToken: Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task FailedSend_DoesNotStopLaterMessagesBeingSent()
    {
        IDiscordWebhookClient client = GetSubstitute<IDiscordWebhookClient>();
        int calls = 0;
        TaskCompletionSource secondSendAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client
            .SendAsync(
                webhookUrl: Arg.Any<Uri>(),
                payload: Arg.Any<WebhookPayload>(),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
            {
                int call = Interlocked.Increment(ref calls);

                if (call == 1)
                {
                    throw new HttpRequestException("connection failed");
                }

                if (call == 2)
                {
                    secondSendAttempted.TrySetResult();
                }

                return new WebhookSendResult(Success: true, StatusCode: HttpStatusCode.NoContent);
            });

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

            await secondSendAttempted.Task.WaitAsync(
                timeout: CompletionWait,
                cancellationToken: this.CancellationToken()
            );

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
