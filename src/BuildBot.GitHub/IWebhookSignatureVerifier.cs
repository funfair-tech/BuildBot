using System;

namespace BuildBot.GitHub;

public interface IWebhookSignatureVerifier
{
    bool IsEnabled { get; }

    bool IsValid(ReadOnlySpan<byte> body, string? signatureHeader);
}
