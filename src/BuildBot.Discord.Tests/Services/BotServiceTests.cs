using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using Discord;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class BotServiceTests : TestBase
{
    private static (
        BotService Service,
        IDiscordBot Bot,
        MessageChannel<BotMessage> MessageChannel,
        MessageChannel<BotReleaseMessage> ReleaseChannel
    ) CreateService()
    {
        IDiscordBot bot = GetSubstitute<IDiscordBot>();
        bot.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        bot.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        MessageChannel<BotMessage> messageChannel = new();
        MessageChannel<BotReleaseMessage> releaseChannel = new();

        BotService service = new(bot: bot, botMessageChannel: messageChannel, botReleaseMessageChannel: releaseChannel);

        return (service, bot, messageChannel, releaseChannel);
    }

    [Fact]
    public async Task StartAsync_DelegatesToBot()
    {
        (BotService service, IDiscordBot bot, _, _) = CreateService();

        using (service)
        {
            await service.StartAsync(this.CancellationToken());

            await bot.Received(1).StartAsync(Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task StopAsync_DelegatesToBot()
    {
        (BotService service, IDiscordBot bot, _, _) = CreateService();

        using (service)
        {
            await service.StopAsync(this.CancellationToken());

            await bot.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public void Dispose_DisposesSubscriptions()
    {
        (BotService service, _, _, _) = CreateService();

        service.Dispose();
    }

    [Fact]
    public async Task BotMessage_IsForwardedToBot_AfterDelay()
    {
        (BotService service, IDiscordBot bot, MessageChannel<BotMessage> messageChannel, _) = CreateService();

        using (service)
        {
            EmbedBuilder builder = new EmbedBuilder().WithTitle("Test");
            BotMessage message = new(builder);

            await messageChannel.PublishAsync(message: message, cancellationToken: this.CancellationToken());

            await Task.Delay(TimeSpan.FromSeconds(2), this.CancellationToken());

            await bot.Received(1)
                .PublishAsync(builder: Arg.Any<EmbedBuilder>(), cancellationToken: Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task BotReleaseMessage_IsForwardedToBot_AfterDelay()
    {
        (BotService service, IDiscordBot bot, _, MessageChannel<BotReleaseMessage> releaseChannel) = CreateService();

        using (service)
        {
            EmbedBuilder builder = new EmbedBuilder().WithTitle("Release");
            BotReleaseMessage message = new(builder);

            await releaseChannel.PublishAsync(message: message, cancellationToken: this.CancellationToken());

            await Task.Delay(TimeSpan.FromSeconds(2), this.CancellationToken());

            await bot.Received(1)
                .PublishToReleaseChannelAsync(
                    builder: Arg.Any<EmbedBuilder>(),
                    cancellationToken: Arg.Any<CancellationToken>()
                );
        }
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBotIsNull()
    {
        ConstructorInfo? ctor = typeof(BotService).GetConstructor(
            [typeof(IDiscordBot), typeof(IMessageChannel<BotMessage>), typeof(IMessageChannel<BotReleaseMessage>)]
        );
        Assert.NotNull(ctor);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke([null, null, null]));
        ArgumentNullException ane = Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal(expected: "bot", actual: ane.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBotMessageChannelIsNull()
    {
        ConstructorInfo? ctor = typeof(BotService).GetConstructor(
            [typeof(IDiscordBot), typeof(IMessageChannel<BotMessage>), typeof(IMessageChannel<BotReleaseMessage>)]
        );
        Assert.NotNull(ctor);

        IDiscordBot bot = GetSubstitute<IDiscordBot>();
        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke([bot, null, null]));
        ArgumentNullException ane = Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal(expected: "botMessageChannel", actual: ane.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBotReleaseMessageChannelIsNull()
    {
        ConstructorInfo? ctor = typeof(BotService).GetConstructor(
            [typeof(IDiscordBot), typeof(IMessageChannel<BotMessage>), typeof(IMessageChannel<BotReleaseMessage>)]
        );
        Assert.NotNull(ctor);

        IDiscordBot bot = GetSubstitute<IDiscordBot>();
        MessageChannel<BotMessage> messageChannel = new();
        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            ctor.Invoke([bot, messageChannel, null])
        );
        ArgumentNullException ane = Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal(expected: "botReleaseMessageChannel", actual: ane.ParamName);
    }
}
