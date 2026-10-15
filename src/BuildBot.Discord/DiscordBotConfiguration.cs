using System;
using System.Diagnostics;

namespace BuildBot.Discord;

[DebuggerDisplay("Webhook configured: {IsConfigured}")]
public sealed record DiscordBotConfiguration(Uri WebhookUrl)
{
    public bool IsConfigured => IsValidWebhookUrl(this.WebhookUrl);

    public static DiscordBotConfiguration FromWebhookUrl(string? webhookUrl)
    {
        if (Uri.TryCreate(uriString: webhookUrl, uriKind: UriKind.Absolute, out Uri? uri) && IsValidWebhookUrl(uri))
        {
            return new(uri);
        }

        throw new InvalidOperationException("Discord:WebhookUrl must be configured as an absolute https URL");
    }

    public override string ToString()
    {
        return $"{nameof(DiscordBotConfiguration)} {{ {nameof(this.IsConfigured)} = {this.IsConfigured} }}";
    }

    private static bool IsValidWebhookUrl(Uri webhookUrl)
    {
        return webhookUrl.IsAbsoluteUri && StringComparer.Ordinal.Equals(x: webhookUrl.Scheme, y: Uri.UriSchemeHttps);
    }
}
