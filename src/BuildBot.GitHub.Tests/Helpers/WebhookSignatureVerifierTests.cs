using System.IO;
using System.Threading.Tasks;
using BuildBot.GitHub.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Helpers;

public sealed class WebhookSignatureVerifierTests : TestBase
{
    [Fact]
    public async Task ValidSignatureIsAcceptedAsync()
    {
        Assert.Equal(
            expected: WebhookSignatureVerificationResult.VALID,
            actual: await this.VerifyAsync(
                secret: GitHubWebhookTestVectors.Secret,
                body: GitHubWebhookTestVectors.Payload.ToArray(),
                signatureHeader: GitHubWebhookTestVectors.ValidSignature
            )
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
    public async Task MalformedSignatureIsRejectedAsync(string signatureHeader)
    {
        Assert.Equal(
            expected: WebhookSignatureVerificationResult.MALFORMED_HEADER,
            actual: await this.VerifyAsync(
                secret: GitHubWebhookTestVectors.Secret,
                body: GitHubWebhookTestVectors.Payload.ToArray(),
                signatureHeader: signatureHeader
            )
        );
    }

    [Fact]
    public async Task MismatchedSignatureIsRejectedAsync()
    {
        Assert.Equal(
            expected: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
            actual: await this.VerifyAsync(
                secret: GitHubWebhookTestVectors.Secret,
                body: GitHubWebhookTestVectors.Payload.ToArray(),
                signatureHeader: GitHubWebhookTestVectors.MismatchedSignature
            )
        );
    }

    [Fact]
    public async Task TamperedBodyIsRejectedAsync()
    {
        Assert.Equal(
            expected: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
            actual: await this.VerifyAsync(
                secret: GitHubWebhookTestVectors.Secret,
                body: "Hello, World?"u8.ToArray(),
                signatureHeader: GitHubWebhookTestVectors.ValidSignature
            )
        );
    }

    [Fact]
    public async Task WrongSecretIsRejectedAsync()
    {
        Assert.Equal(
            expected: WebhookSignatureVerificationResult.SIGNATURE_MISMATCH,
            actual: await this.VerifyAsync(
                secret: "Another secret",
                body: GitHubWebhookTestVectors.Payload.ToArray(),
                signatureHeader: GitHubWebhookTestVectors.ValidSignature
            )
        );
    }

    private async Task<WebhookSignatureVerificationResult> VerifyAsync(
        string secret,
        byte[] body,
        string signatureHeader
    )
    {
        WebhookSignatureVerifier verifier = new(new GitHubConfiguration(secret));

        await using (MemoryStream stream = new(body))
        {
            return await verifier.VerifyAsync(
                body: stream,
                signatureHeader: signatureHeader,
                cancellationToken: this.CancellationToken()
            );
        }
    }
}
