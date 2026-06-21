using BuildBot.Discord;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.ServiceModel.ComponentStatus;
using Discord;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BuildBot.Discord.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    private static readonly DiscordBotConfiguration TestConfig = new(
        token: "test-token",
        server: "test-server",
        channel: "test-channel",
        releaseChannel: "test-release-channel"
    );

    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddDiscord(TestConfig);
    }

    [Fact]
    public void DiscordBotMustBeRegisteredAsIDiscordBot()
    {
        this.RequireService<IDiscordBot>();
    }

    [Fact]
    public void DiscordBotMustBeRegisteredAsIComponentStatus()
    {
        this.RequireService<IComponentStatus>();
    }

    [Fact]
    public void BotMessageChannelMustBeRegistered()
    {
        this.RequireService<IMessageChannel<BotMessage>>();
    }

    [Fact]
    public void BotReleaseMessageChannelMustBeRegistered()
    {
        this.RequireService<IMessageChannel<BotReleaseMessage>>();
    }

    [Fact]
    public void BotServiceMustBeRegisteredAsIHostedService()
    {
        this.RequireService<IHostedService>();
    }

    [Fact]
    public void DiscordRawClientMustBeRegistered()
    {
        this.RequireService<IDiscordRawClient>();
    }

    [Fact]
    public void DiscordRawClient_WhenDisconnected_LoginStateIsLoggedOut()
    {
        IDiscordRawClient client = this.GetService<IDiscordRawClient>();
        Assert.Equal(expected: LoginState.LoggedOut, actual: client.LoginState);
    }

    [Fact]
    public void FindChannel_WhenClientIsDisconnected_ReturnsNull()
    {
        IDiscordRawClient client = this.GetService<IDiscordRawClient>();
        IDiscordChannel? channel = client.FindChannel(
            serverName: "nonexistent-server",
            channelName: "nonexistent-channel"
        );
        Assert.Null(channel);
    }
}
