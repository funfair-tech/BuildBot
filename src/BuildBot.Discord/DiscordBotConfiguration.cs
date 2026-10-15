using System;
using System.Diagnostics;

namespace BuildBot.Discord;

[DebuggerDisplay("Webhook: <redacted>")]
public sealed record DiscordBotConfiguration
{
    private DiscordBotConfiguration(Uri webhookUrl)
    {
        this.WebhookUrl = webhookUrl;
    }

    public Uri WebhookUrl { get; }

    public static DiscordBotConfiguration FromWebhookUrl(string? webhookUrl)
    {
        if (
            Uri.TryCreate(uriString: webhookUrl, uriKind: UriKind.Absolute, out Uri? uri)
            && StringComparer.Ordinal.Equals(x: uri.Scheme, y: Uri.UriSchemeHttps)
        )
        {
            return new(uri);
        }

        throw new InvalidOperationException("Discord:WebhookUrl must be configured as an absolute https URL");
    }

    public override string ToString()
    {
        return $"{nameof(DiscordBotConfiguration)} {{ Webhook = <redacted> }}";
    }
}
