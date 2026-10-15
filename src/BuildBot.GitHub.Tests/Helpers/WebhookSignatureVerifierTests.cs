using BuildBot.GitHub.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Helpers;

public sealed class WebhookSignatureVerifierTests : TestBase
{
    [Fact]
    public static void IsEnabledWhenSecretConfigured()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(GitHubWebhookTestVectors.Secret));

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
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(GitHubWebhookTestVectors.Secret));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.VALID,
            actual: verifier.Verify(
                body: GitHubWebhookTestVectors.Payload,
                signatureHeader: GitHubWebhookTestVectors.ValidSignature
            )
        );
    }

    [Fact]
    public static void MissingSignatureIsRejected()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(GitHubWebhookTestVectors.Secret));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.MISSING_HEADER,
            actual: verifier.Verify(body: GitHubWebhookTestVectors.Payload, signatureHeader: null)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha1=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("SHA256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha256=zz7107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17")]
    [InlineData("sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e1")]
    [InlineData("sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e1700")]
    [InlineData("sha256=")]
    public static void MalformedSignatureIsRejected(string signatureHeader)
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(GitHubWebhookTestVectors.Secret));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.MALFORMED_HEADER,
            actual: verifier.Verify(body: GitHubWebhookTestVectors.Payload, signatureHeader: signatureHeader)
        );
    }

    [Fact]
    public static void MismatchedSignatureIsRejected()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(GitHubWebhookTestVectors.Secret));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
            actual: verifier.Verify(
                body: GitHubWebhookTestVectors.Payload,
                signatureHeader: GitHubWebhookTestVectors.MismatchedSignature
            )
        );
    }

    [Fact]
    public static void TamperedBodyIsRejected()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(GitHubWebhookTestVectors.Secret));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
            actual: verifier.Verify(body: "Hello, World?"u8, signatureHeader: GitHubWebhookTestVectors.ValidSignature)
        );
    }

    [Fact]
    public static void WrongSecretIsRejected()
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration("Another secret"));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
            actual: verifier.Verify(
                body: GitHubWebhookTestVectors.Payload,
                signatureHeader: GitHubWebhookTestVectors.ValidSignature
            )
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sha256=not-a-signature")]
    [InlineData(GitHubWebhookTestVectors.ValidSignature)]
    public static void EmptySecretAcceptsAnySignature(string? signatureHeader)
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(string.Empty));

        Assert.Equal(
            expected: WebhookSignatureVerificationResult.VALID,
            actual: verifier.Verify(body: GitHubWebhookTestVectors.Payload, signatureHeader: signatureHeader)
        );
    }
}
