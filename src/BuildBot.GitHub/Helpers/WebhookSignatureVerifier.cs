using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace BuildBot.GitHub.Helpers;

public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private const string SIGNATURE_PREFIX = "sha256=";
    private const int SIGNATURE_HEX_LENGTH = HMACSHA256.HashSizeInBytes * 2;

    private readonly byte[] _key;

    public WebhookSignatureVerifier(GitHubConfiguration configuration)
    {
        this.IsEnabled = configuration.HasWebhookSecret;
        this._key = Encoding.UTF8.GetBytes(configuration.WebhookSecret);
    }

    public bool IsEnabled { get; }

    public bool IsValid(ReadOnlySpan<byte> body, string? signatureHeader)
    {
        if (!this.IsEnabled)
        {
            return true;
        }

        if (
            signatureHeader is null
            || !signatureHeader.StartsWith(value: SIGNATURE_PREFIX, comparisonType: StringComparison.Ordinal)
        )
        {
            return false;
        }

        ReadOnlySpan<char> hex = signatureHeader.AsSpan(SIGNATURE_PREFIX.Length);

        if (hex.Length != SIGNATURE_HEX_LENGTH)
        {
            return false;
        }

        Span<byte> provided = stackalloc byte[HMACSHA256.HashSizeInBytes];

        OperationStatus status = Convert.FromHexString(
            source: hex,
            destination: provided,
            charsConsumed: out _,
            bytesWritten: out _
        );

        if (status != OperationStatus.Done)
        {
            return false;
        }

        Span<byte> expected = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(key: this._key, source: body, destination: expected);

        return CryptographicOperations.FixedTimeEquals(left: expected, right: provided);
    }
}
