using BuildBot.Discord.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Models;

public sealed class BotMessageTests : TestBase
{
    [Fact]
    public void MessageProperty_ReturnsSuppliedEmbed()
    {
        DiscordEmbed embed = TestEmbeds.WithTitle("Message");
        BotMessage msg = new(embed);

        Assert.Same(expected: embed, actual: msg.Message);
    }

    [Fact]
    public void TwoInstances_WithSameEmbed_AreEqual()
    {
        DiscordEmbed embed = TestEmbeds.WithTitle("Message");
        BotMessage msg1 = new(embed);
        BotMessage msg2 = new(embed);

        Assert.Equal(expected: msg1, actual: msg2);
    }

    [Fact]
    public void TwoInstances_WithDifferentEmbeds_AreNotEqual()
    {
        BotMessage msg1 = new(TestEmbeds.WithTitle("First Message"));
        BotMessage msg2 = new(TestEmbeds.WithTitle("Second Message"));

        Assert.NotEqual(expected: msg1, actual: msg2);
    }
}
