using System;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests;

public sealed class GitHubConfigurationTests : TestBase
{
    private const string SECRET = "It's a Secret to Everybody";

    [Fact]
    public static void HasWebhookSecretWhenSecretSet()
    {
        GitHubConfiguration config = new(SECRET);

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
        GitHubConfiguration config = new(SECRET);

        string text = config.ToString();

        Assert.DoesNotContain(expectedSubstring: SECRET, actualString: text, comparisonType: StringComparison.Ordinal);
        Assert.Equal(expected: "GitHubConfiguration { HasWebhookSecret = True }", actual: text);
    }
}
