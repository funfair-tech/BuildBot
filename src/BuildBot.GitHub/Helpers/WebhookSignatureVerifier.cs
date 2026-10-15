using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub.Helpers;

public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private const string SIGNATURE_PREFIX = "sha256=";
    private const int SIGNATURE_HEX_LENGTH = HMACSHA256.HashSizeInBytes * 2;

    private readonly byte[] _key;

    public WebhookSignatureVerifier(GitHubConfiguration configuration)
    {
        this._key = Encoding.UTF8.GetBytes(configuration.WebhookSecret);
    }

    public async ValueTask<WebhookSignatureVerificationResult> VerifyAsync(
        Stream body,
        string signatureHeader,
        CancellationToken cancellationToken
    )
    {
        // Parse the header first so a malformed one never causes the body to be read.
        byte[]? provided = DecodeSignature(signatureHeader);

        if (provided is null)
        {
            return WebhookSignatureVerificationResult.MALFORMED_HEADER;
        }

        byte[] expected = await HMACSHA256.HashDataAsync(
            key: this._key,
            source: body,
            cancellationToken: cancellationToken
        );

        return CryptographicOperations.FixedTimeEquals(left: expected, right: provided)
            ? WebhookSignatureVerificationResult.VALID
            : WebhookSignatureVerificationResult.SIGNATURE_MISMATCH;
    }

    private static byte[]? DecodeSignature(string signatureHeader)
    {
        if (!signatureHeader.StartsWith(value: SIGNATURE_PREFIX, comparisonType: StringComparison.Ordinal))
        {
            return null;
        }

        ReadOnlySpan<char> hex = signatureHeader.AsSpan(SIGNATURE_PREFIX.Length);

        if (hex.Length != SIGNATURE_HEX_LENGTH)
        {
            return null;
        }

        byte[] provided = new byte[HMACSHA256.HashSizeInBytes];

        OperationStatus status = Convert.FromHexString(
            source: hex,
            destination: provided,
            charsConsumed: out _,
            bytesWritten: out _
        );

        return status == OperationStatus.Done ? provided : null;
    }
}
