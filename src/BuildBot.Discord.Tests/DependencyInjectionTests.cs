using System;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.ServiceModel.ComponentStatus;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BuildBot.Discord.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    private static readonly DiscordBotConfiguration TestConfig = new(new Uri("https://example.com/webhook"));

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
    public void DiscordWebhookClientMustBeRegistered()
    {
        this.RequireService<IDiscordWebhookClient>();
    }

    [Fact]
    public void TimeProviderMustBeRegistered()
    {
        this.RequireService<TimeProvider>();
    }

    [Fact]
    public void ConfigurationMustBeRegistered()
    {
        this.RequireService<DiscordBotConfiguration>();
    }

    [Fact]
    public void BotMessageChannelMustBeRegistered()
    {
        this.RequireService<IMessageChannel<BotMessage>>();
    }

    [Fact]
    public void BotServiceMustBeRegisteredAsIHostedService()
    {
        this.RequireService<IHostedService>();
    }
}
