using System;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests;

public sealed class GitHubConfigurationTests : TestBase
{
    [Fact]
    public static void HasWebhookSecretWhenSecretSet()
    {
        GitHubConfiguration config = new(GitHubWebhookTestVectors.Secret);

        Assert.True(config.HasWebhookSecret, userMessage: "Should have webhook secret");
    }

    [Fact]
    public static void HasNoWebhookSecretWhenSecretEmpty()
    {
        GitHubConfiguration config = new(string.Empty);

        Assert.False(config.HasWebhookSecret, userMessage: "Should not have webhook secret");
    }

    [Fact]
    public static void ToStringDoesNotExposeSecret()
    {
        GitHubConfiguration config = new(GitHubWebhookTestVectors.Secret);

        string text = config.ToString();

        Assert.DoesNotContain(
            expectedSubstring: GitHubWebhookTestVectors.Secret,
            actualString: text,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Equal(expected: "GitHubConfiguration { HasWebhookSecret = True }", actual: text);
    }
}
