using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.Discord.Tests;

public sealed class DiscordSetupConfigurationTests : TestBase
{
    private const string WEBHOOK_URL_KEY = "Discord:WebhookUrl";
    private const string WEBHOOK_URL = "https://example.com/webhook";

    [Fact]
    public async Task PresentWebhookUrlIsBoundAsync()
    {
        await using ServiceProvider serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>(StringComparer.Ordinal) { [WEBHOOK_URL_KEY] = WEBHOOK_URL }
        );

        DiscordBotConfiguration discordConfig = serviceProvider.GetRequiredService<DiscordBotConfiguration>();

        Assert.Equal(expected: new Uri(WEBHOOK_URL), actual: discordConfig.WebhookUrl);
    }

    [Fact]
    public async Task PresentWebhookUrlResolvesDiscordBotAsync()
    {
        await using ServiceProvider serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>(StringComparer.Ordinal) { [WEBHOOK_URL_KEY] = WEBHOOK_URL }
        );

        Assert.NotNull(serviceProvider.GetRequiredService<IDiscordBot>());
    }

    [Fact]
    public void MissingWebhookUrlThrows()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal));

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddDiscord(configuration));
    }

    [Fact]
    public void EmptyWebhookUrlThrows()
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>(StringComparer.Ordinal) { [WEBHOOK_URL_KEY] = string.Empty }
        );

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddDiscord(configuration));
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> settings)
    {
        return new ServiceCollection().AddDiscord(BuildConfiguration(settings)).BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> settings)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}
