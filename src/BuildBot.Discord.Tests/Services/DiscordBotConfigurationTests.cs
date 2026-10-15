using System;
using System.Diagnostics;
using System.Reflection;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordBotConfigurationTests : TestBase
{
    private const string WEBHOOK_URL = "https://example.com/webhook/secret-token";

    [Fact]
    public void FromWebhookUrl_WithHttpsUrl_ReturnsConfiguredConfiguration()
    {
        DiscordBotConfiguration config = DiscordBotConfiguration.FromWebhookUrl(WEBHOOK_URL);

        Assert.Equal(expected: new Uri(WEBHOOK_URL), actual: config.WebhookUrl);
        Assert.True(condition: config.IsConfigured, userMessage: "Https webhook URL should be configured");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/webhook/secret-token")]
    [InlineData("webhook/secret-token")]
    [InlineData("http://example.com/webhook/secret-token")]
    [InlineData("ftp://example.com/webhook/secret-token")]
    public static void FromWebhookUrl_WithMissingOrInvalidUrl_ThrowsWithoutRevealingValue(string? webhookUrl)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            DiscordBotConfiguration.FromWebhookUrl(webhookUrl)
        );

        Assert.Equal(
            expected: "Discord:WebhookUrl must be configured as an absolute https URL",
            actual: exception.Message
        );
        Assert.DoesNotContain(
            expectedSubstring: "secret-token",
            actualString: exception.ToString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData("https://example.com/webhook/secret-token", true)]
    [InlineData("http://example.com/webhook/secret-token", false)]
    public static void IsConfigured_ReflectsWhetherUrlIsAbsoluteHttps(string webhookUrl, bool expected)
    {
        DiscordBotConfiguration config = new(new Uri(webhookUrl));

        Assert.Equal(expected: expected, actual: config.IsConfigured);
    }

    [Fact]
    public void IsConfigured_WithRelativeUrl_IsFalse()
    {
        DiscordBotConfiguration config = new(new Uri(uriString: "webhook/secret-token", uriKind: UriKind.Relative));

        Assert.False(condition: config.IsConfigured, userMessage: "Relative webhook URL should not be configured");
    }

    [Fact]
    public void ToString_DoesNotRevealWebhookUrl()
    {
        DiscordBotConfiguration config = DiscordBotConfiguration.FromWebhookUrl(WEBHOOK_URL);

        string text = config.ToString();

        Assert.Equal(expected: "DiscordBotConfiguration { IsConfigured = True }", actual: text);
    }

    [Fact]
    public void DebuggerDisplay_DoesNotReferenceWebhookUrl()
    {
        DebuggerDisplayAttribute attribute = Assert.Single(
            typeof(DiscordBotConfiguration).GetCustomAttributes<DebuggerDisplayAttribute>()
        );

        Assert.DoesNotContain(
            expectedSubstring: nameof(DiscordBotConfiguration.WebhookUrl),
            actualString: attribute.Value,
            comparisonType: StringComparison.Ordinal
        );
    }
}
