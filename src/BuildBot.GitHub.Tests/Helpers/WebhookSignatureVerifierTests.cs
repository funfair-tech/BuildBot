using System;
using BuildBot.GitHub.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Helpers;

public sealed class WebhookSignatureVerifierTests : TestBase
{
    // Test vector from GitHub's "Validating webhook deliveries" documentation.
    private const string SECRET = "It's a Secret to Everybody";
    private const string VALID_SIGNATURE = "sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17";

    private static ReadOnlySpan<byte> Payload => "Hello, World!"u8;

    [Fact]
    public static void IsEnabledWhenSecretConfigured()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(SECRET));

        Assert.True(verifier.IsEnabled, userMessage: "Should be enabled");
    }

    [Fact]
    public static void IsNotEnabledWhenSecretEmpty()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(string.Empty));

        Assert.False(verifier.IsEnabled, userMessage: "Should not be enabled");
    }

    [Fact]
    public static void ValidSignatureIsAccepted()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(SECRET));

        Assert.True(verifier.IsValid(body: Payload, signatureHeader: VALID_SIGNATURE), userMessage: "Should be valid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha1=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("SHA256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha256=057107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha256=zz7107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e1")]
    [InlineData("sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e1700")]
    [InlineData("sha256=")]
    public static void InvalidSignatureIsRejected(string? signatureHeader)
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(SECRET));

        Assert.False(
            verifier.IsValid(body: Payload, signatureHeader: signatureHeader),
            userMessage: "Should be rejected"
        );
    }

    [Fact]
    public static void TamperedBodyIsRejected()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(SECRET));

        Assert.False(
            verifier.IsValid(body: "Hello, World?"u8, signatureHeader: VALID_SIGNATURE),
            userMessage: "Should be rejected"
        );
    }

    [Fact]
    public static void WrongSecretIsRejected()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration("Another secret"));

        Assert.False(
            verifier.IsValid(body: Payload, signatureHeader: VALID_SIGNATURE),
            userMessage: "Should be rejected"
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sha256=not-a-signature")]
    [InlineData(VALID_SIGNATURE)]
    public static void EmptySecretAcceptsAnySignature(string? signatureHeader)
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(string.Empty));

        Assert.True(
            verifier.IsValid(body: Payload, signatureHeader: signatureHeader),
            userMessage: "Should pass through"
        );
    }
}
