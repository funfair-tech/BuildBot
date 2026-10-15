using BuildBot.Discord.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Models;

public sealed class DiscordColorTests : TestBase
{
    [Fact]
    public void Green_MatchesDiscordGreen()
    {
        Assert.Equal(expected: 0x2ECC71u, actual: DiscordColor.Green.Value);
    }

    [Fact]
    public void Red_MatchesDiscordRed()
    {
        Assert.Equal(expected: 0xE74C3Cu, actual: DiscordColor.Red.Value);
    }
}
