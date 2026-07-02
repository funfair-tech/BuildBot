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
        message.CleanContent.Returns("clean content");

        channel
            .SendMessageAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<Embed>(),
                Arg.Any<RequestOptions>(),
                Arg.Any<AllowedMentions>(),
                Arg.Any<MessageReference>(),
                Arg.Any<MessageComponent>(),
                Arg.Any<ISticker[]>(),
                Arg.Any<Embed[]>(),
                Arg.Any<MessageFlags>(),
                Arg.Any<PollProperties>()
            )
            .Returns(Task.FromResult(message));

        DiscordChannelAdapter adapter = new(channel);

        Embed embed = new EmbedBuilder().Build();
        (string sentToChannel, string messageContent) = await adapter.SendMessageAsync(embed);

        Assert.Equal(expected: "sent-channel", actual: sentToChannel);
        Assert.Equal(expected: "clean content", actual: messageContent);
    }
}
