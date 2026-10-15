using System;

namespace BuildBot.GitHub;

public interface IWebhookSignatureVerifier
{
    bool IsEnabled { get; }

    WebhookSignatureVerificationResult Verify(ReadOnlySpan<byte> body, string? signatureHeader);
}
