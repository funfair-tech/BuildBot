using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.GitHub.Tests;

public sealed class GitHubSetupConfigurationTests : TestBase
{
    private const string WEBHOOK_SECRET_KEY = "GitHub:WebhookSecret";

    [Fact]
    public async Task PresentWebhookSecretIsBoundAsync()
    {
        GitHubConfiguration gitHubConfig = await ResolveConfigurationAsync(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [WEBHOOK_SECRET_KEY] = GitHubWebhookTestVectors.Secret,
            }
        );

        Assert.Equal(expected: GitHubWebhookTestVectors.Secret, actual: gitHubConfig.WebhookSecret);
        Assert.True(gitHubConfig.HasWebhookSecret, userMessage: "Webhook secret should be configured");
    }

    [Fact]
    public async Task MissingWebhookSecretBindsToEmptyAsync()
    {
        GitHubConfiguration gitHubConfig = await ResolveConfigurationAsync(
            new Dictionary<string, string?>(StringComparer.Ordinal)
        );

        Assert.Equal(expected: string.Empty, actual: gitHubConfig.WebhookSecret);
        Assert.False(gitHubConfig.HasWebhookSecret, userMessage: "Webhook secret should not be configured");
    }

    [Fact]
    public async Task EmptyWebhookSecretBindsToEmptyAsync()
    {
        GitHubConfiguration gitHubConfig = await ResolveConfigurationAsync(
            new Dictionary<string, string?>(StringComparer.Ordinal) { [WEBHOOK_SECRET_KEY] = string.Empty }
        );

        Assert.Equal(expected: string.Empty, actual: gitHubConfig.WebhookSecret);
        Assert.False(gitHubConfig.HasWebhookSecret, userMessage: "Webhook secret should not be configured");
    }

    private static async ValueTask<GitHubConfiguration> ResolveConfigurationAsync(
        IReadOnlyDictionary<string, string?> settings
    )
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        await using ServiceProvider serviceProvider = new ServiceCollection()
            .AddMockedService<IMediator>()
            .AddGitHub(configuration)
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<GitHubConfiguration>();
    }
}
