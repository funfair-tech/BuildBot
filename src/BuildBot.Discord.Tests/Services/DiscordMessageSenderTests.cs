using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordMessageSenderTests : TestBase
{
    private const int PUBLISH_FAILED_EVENT_ID = 7;
    private static readonly TimeSpan CompletionWait = TimeSpan.FromSeconds(10);

    private static (
        DiscordMessageSender Service,
        IDiscordBot Bot,
        MessageChannel<BotMessage> MessageChannel,
        CapturingLogger<DiscordMessageSender> Logger
    ) CreateService()
    {
        IDiscordBot bot = GetSubstitute<IDiscordBot>();
        MessageChannel<BotMessage> messageChannel = new();
        CapturingLogger<DiscordMessageSender> logger = new();

        DiscordMessageSender service = new(bot: bot, botMessageChannel: messageChannel, logger: logger);

        return (service, bot, messageChannel, logger);
    }

    [Fact]
    public async Task StartAsync_DoesNotPublishWithoutMessages()
    {
        (DiscordMessageSender service, IDiscordBot bot, _, _) = CreateService();

        using (service)
        {
            await service.StartAsync(this.CancellationToken());
            await service.StopAsync(this.CancellationToken());

            Assert.Empty(bot.ReceivedCalls());
        }
    }

    [Fact]
    public async Task StopAsync_EndsTheLoopAndStopsForwardingMessages()
    {
        (DiscordMessageSender service, IDiscordBot bot, MessageChannel<BotMessage> messageChannel, _) = CreateService();

        using (service)
        {
            CancellationToken cancellationToken = this.CancellationToken();
            await service.StartAsync(cancellationToken);
            await service.StopAsync(cancellationToken);

            Assert.NotNull(service.ExecuteTask);
            Assert.True(service.ExecuteTask.IsCompleted, userMessage: "The send loop should have ended");

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
    public async Task BotMessage_IsForwardedToBot()
    {
        (DiscordMessageSender service, IDiscordBot bot, MessageChannel<BotMessage> messageChannel, _) = CreateService();
        TaskCompletionSource published = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bot.When(async x =>
                await x.PublishAsync(embed: Arg.Any<DiscordEmbed>(), cancellationToken: Arg.Any<CancellationToken>())
            )
            .Do(_ => published.TrySetResult());

        using (service)
        {
            CancellationToken cancellationToken = this.CancellationToken();
            await service.StartAsync(cancellationToken);

            DiscordEmbed embed = TestEmbeds.WithTitle("Test");

            await messageChannel.PublishAsync(message: new(embed), cancellationToken: cancellationToken);

            await published.Task.WaitAsync(timeout: CompletionWait, cancellationToken: cancellationToken);

            await service.StopAsync(cancellationToken);

            await bot.Received(1).PublishAsync(embed: embed, cancellationToken: Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task ChannelCompletion_EndsTheLoopAfterSendingEveryMessage()
    {
        IDiscordBot bot = GetSubstitute<IDiscordBot>();
        IMessageChannel<BotMessage> messageChannel = GetSubstitute<IMessageChannel<BotMessage>>();
        DiscordEmbed embed = TestEmbeds.WithTitle("Test");
        messageChannel
            .ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(new BotMessage[] { new(embed) }.ToAsyncEnumerable());

        using (
            DiscordMessageSender service = new(
                bot: bot,
                botMessageChannel: messageChannel,
                logger: new CapturingLogger<DiscordMessageSender>()
            )
        )
        {
            CancellationToken cancellationToken = this.CancellationToken();
            await service.StartAsync(cancellationToken);

            Assert.NotNull(service.ExecuteTask);
            await service.ExecuteTask.WaitAsync(timeout: CompletionWait, cancellationToken: cancellationToken);

            await service.StopAsync(cancellationToken);

            await bot.Received(1).PublishAsync(embed: embed, cancellationToken: Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task StopAsync_DuringSend_EndsTheLoopWithoutLoggingAFailure()
    {
        (
            DiscordMessageSender service,
            IDiscordBot bot,
            MessageChannel<BotMessage> messageChannel,
            CapturingLogger<DiscordMessageSender> logger
        ) = CreateService();
        TaskCompletionSource sendStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bot.When(async x =>
                await x.PublishAsync(embed: Arg.Any<DiscordEmbed>(), cancellationToken: Arg.Any<CancellationToken>())
            )
            .Do(call =>
                BlockUntilCancelled(sendStarted: sendStarted, cancellationToken: call.Arg<CancellationToken>())
            );

        using (service)
        {
            CancellationToken cancellationToken = this.CancellationToken();
            await service.StartAsync(cancellationToken);

            await messageChannel.PublishAsync(
                message: new(TestEmbeds.WithTitle("Test")),
                cancellationToken: cancellationToken
            );
            await sendStarted.Task.WaitAsync(timeout: CompletionWait, cancellationToken: cancellationToken);

            await service.StopAsync(cancellationToken);

            Assert.NotNull(service.ExecuteTask);
            Assert.True(service.ExecuteTask.IsCanceled, userMessage: "The send loop should have ended by cancellation");
            Assert.Empty(logger.Entries);
        }
    }

    private static void BlockUntilCancelled(TaskCompletionSource sendStarted, in CancellationToken cancellationToken)
    {
        sendStarted.TrySetResult();

        // Bounded so a broken stop fails the IsCanceled assertion rather than hanging the run.
        cancellationToken.WaitHandle.WaitOne(CompletionWait);
        cancellationToken.ThrowIfCancellationRequested();
    }

    [Fact]
    public async Task FailedPublish_IsLoggedAndLaterMessagesAreStillSent()
    {
        (
            DiscordMessageSender service,
            IDiscordBot bot,
            MessageChannel<BotMessage> messageChannel,
            CapturingLogger<DiscordMessageSender> logger
        ) = CreateService();
        TaskCompletionSource secondPublishAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bot.When(async x =>
                await x.PublishAsync(embed: Arg.Any<DiscordEmbed>(), cancellationToken: Arg.Any<CancellationToken>())
            )
            .Do(
                Callback
                    .First(_ => throw new HttpRequestException("connection failed"))
                    .Then(_ => secondPublishAttempted.TrySetResult())
            );

        using (service)
        {
            CancellationToken cancellationToken = this.CancellationToken();
            await service.StartAsync(cancellationToken);

            DiscordEmbed second = TestEmbeds.WithTitle("Second");
            await messageChannel.PublishAsync(
                message: new(TestEmbeds.WithTitle("First")),
                cancellationToken: cancellationToken
            );
            await messageChannel.PublishAsync(message: new(second), cancellationToken: cancellationToken);

            await secondPublishAttempted.Task.WaitAsync(timeout: CompletionWait, cancellationToken: cancellationToken);

            await service.StopAsync(cancellationToken);

            await bot.Received(1).PublishAsync(embed: second, cancellationToken: Arg.Any<CancellationToken>());

            // Messages are sent one at a time, so the first failure was logged before the second send began.
            CapturedLogEntry failure = Assert.Single(
                collection: logger.Entries,
                predicate: static entry => entry.Level == LogLevel.Error
            );
            Assert.Equal(expected: PUBLISH_FAILED_EVENT_ID, actual: failure.EventId.Id);
            Assert.Contains(
                expectedSubstring: "First",
                actualString: failure.Message,
                comparisonType: StringComparison.Ordinal
            );
        }
    }
}
