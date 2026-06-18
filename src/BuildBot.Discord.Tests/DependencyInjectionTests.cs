using BuildBot.Discord;
using BuildBot.Discord.Models;
using BuildBot.ServiceModel.ComponentStatus;
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
}
