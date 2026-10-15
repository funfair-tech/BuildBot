using System.Diagnostics;

namespace BuildBot.GitHub;

[DebuggerDisplay("HasWebhookSecret: {HasWebhookSecret}")]
public sealed record GitHubConfiguration(string WebhookSecret)
{
    public bool HasWebhookSecret => !string.IsNullOrEmpty(this.WebhookSecret);

    // The synthesised record ToString would include the secret.
    public override string ToString()
    {
        return $"GitHubConfiguration {{ HasWebhookSecret = {this.HasWebhookSecret} }}";
    }
}
