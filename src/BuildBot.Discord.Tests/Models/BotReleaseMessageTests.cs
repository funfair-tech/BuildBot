using BuildBot.Discord.Models;
using Discord;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Models;

public sealed class BotReleaseMessageTests : TestBase
{
    [Fact]
    public void MessageProperty_ReturnsSuppliedBuilder()
    {
        EmbedBuilder builder = new();
        BotReleaseMessage msg = new(builder);

        Assert.Same(expected: builder, actual: msg.Message);
    }

    [Fact]
    public void TwoInstances_WithSameBuilder_AreEqual()
    {
        EmbedBuilder builder = new();
        BotReleaseMessage msg1 = new(builder);
        BotReleaseMessage msg2 = new(builder);

        Assert.Equal(expected: msg1, actual: msg2);
    }

    [Fact]
    public void TwoInstances_WithDifferentBuilders_AreNotEqual()
    {
        BotReleaseMessage msg1 = new(new EmbedBuilder().WithTitle("First Release"));
        BotReleaseMessage msg2 = new(new EmbedBuilder().WithTitle("Second Release"));

        Assert.NotEqual(expected: msg1, actual: msg2);
    }
}
