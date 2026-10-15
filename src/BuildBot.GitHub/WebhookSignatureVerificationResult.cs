namespace BuildBot.GitHub;

public enum WebhookSignatureVerificationResult
{
    VALID,
    MISSING_HEADER,
    MALFORMED_HEADER,
    SIGNATURE_MISMATCH,
    MULTIPLE_HEADERS,
    BODY_TOO_LARGE,
}
