using BuildBot.Discord;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordBotConfigurationTests : TestBase
{
    [Fact]
    public void Token_ReturnsConstructorValue()
    {
        DiscordBotConfiguration config = new(
            token: "my-token",
            server: "server",
            channel: "channel",
            releaseChannel: "releases"
        );

        Assert.Equal(expected: "my-token", actual: config.Token);
    }

    [Fact]
    public void Server_ReturnsConstructorValue()
    {
        DiscordBotConfiguration config = new(
            token: "token",
            server: "my-server",
            channel: "channel",
            releaseChannel: "releases"
        );

        Assert.Equal(expected: "my-server", actual: config.Server);
    }

    [Fact]
    public void Channel_ReturnsConstructorValue()
    {
        DiscordBotConfiguration config = new(
            token: "token",
            server: "server",
            channel: "my-channel",
            releaseChannel: "releases"
        );

        Assert.Equal(expected: "my-channel", actual: config.Channel);
    }

    [Fact]
    public void ReleaseChannel_ReturnsConstructorValue()
    {
        DiscordBotConfiguration config = new(
            token: "token",
            server: "server",
            channel: "channel",
            releaseChannel: "my-releases"
        );

        Assert.Equal(expected: "my-releases", actual: config.ReleaseChannel);
    }
}
