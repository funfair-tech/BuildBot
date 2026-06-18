using BuildBot.Discord.Models;
using Discord;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Models;

public sealed class BotMessageTests : TestBase
{
    [Fact]
    public void MessageProperty_ReturnsSuppliedBuilder()
    {
        EmbedBuilder builder = new();
        BotMessage msg = new(builder);

        Assert.Same(expected: builder, actual: msg.Message);
    }

    [Fact]
    public void TwoInstances_WithSameBuilder_AreEqual()
    {
        EmbedBuilder builder = new();
        BotMessage msg1 = new(builder);
        BotMessage msg2 = new(builder);

        Assert.Equal(expected: msg1, actual: msg2);
    }

    [Fact]
    public void TwoInstances_WithDifferentBuilders_AreNotEqual()
    {
        BotMessage msg1 = new(new EmbedBuilder().WithTitle("First Message"));
        BotMessage msg2 = new(new EmbedBuilder().WithTitle("Second Message"));

        Assert.NotEqual(expected: msg1, actual: msg2);
    }
}
