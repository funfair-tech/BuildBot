using System;

namespace BuildBot.GitHub.Tests;

// Test vector from GitHub's "Validating webhook deliveries" documentation.
internal static class GitHubWebhookTestVectors
{
    public const string Secret = "It's a Secret to Everybody";
    public const string ValidSignature = "sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17";
    public const string MismatchedSignature = "sha256=057107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17";

    public static ReadOnlySpan<byte> Payload => "Hello, World!"u8;
}
