using System;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using Discord;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordChannelAdapterTests : TestBase
{
    [Fact]
    public void Name_ReturnsChannelName()
    {
        ITextChannel channel = GetSubstitute<ITextChannel>();
        channel.Name.Returns("test-channel");

        DiscordChannelAdapter adapter = new(channel);

        Assert.Equal(expected: "test-channel", actual: adapter.Name);
    }

    [Fact]
    public void EnterTypingState_ReturnsTypingStateFromChannel()
    {
        ITextChannel channel = GetSubstitute<ITextChannel>();
        IDisposable typingState = GetSubstitute<IDisposable>();
        channel.EnterTypingState(Arg.Any<RequestOptions>()).Returns(typingState);

        DiscordChannelAdapter adapter = new(channel);

        IDisposable result = adapter.EnterTypingState();

        Assert.Same(expected: typingState, actual: result);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsChannelNameAndCleanContent()
    {
        ITextChannel channel = GetSubstitute<ITextChannel>();
        IUserMessage message = GetSubstitute<IUserMessage>();

        channel.Name.Returns("sent-channel");
        message.Channel.Returns(channel);

        // In production CleanContent is always "" because the adapter sends text: string.Empty (embed-only).
        // This stub verifies the adapter correctly passes through whatever CleanContent the message returns.
        message.CleanContent.Returns("clean content");

        channel.SendMessageAsync(text: string.Empty, embed: default).ReturnsForAnyArgs(Task.FromResult(message));

        DiscordChannelAdapter adapter = new(channel);

        Embed embed = new EmbedBuilder().Build();
        (string sentToChannel, string messageContent) = await adapter.SendMessageAsync(embed);

        Assert.Equal(expected: "sent-channel", actual: sentToChannel);
        Assert.Equal(expected: "clean content", actual: messageContent);

        await channel
            .Received(1)
            .SendMessageAsync(
                text: string.Empty,
                isTTS: Arg.Any<bool>(),
                embed: embed,
                options: Arg.Any<RequestOptions>(),
                allowedMentions: Arg.Any<AllowedMentions>(),
                messageReference: Arg.Any<MessageReference>(),
                components: Arg.Any<MessageComponent>(),
                stickers: Arg.Any<ISticker[]>(),
                embeds: Arg.Any<Embed[]>(),
                flags: Arg.Any<MessageFlags>(),
                poll: Arg.Any<PollProperties>()
            );
    }
}
