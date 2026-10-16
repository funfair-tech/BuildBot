using System;
using System.Linq;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

public static class EmbedMapper
{
    // Discord trims field names and values and rejects the whole message when either ends up empty, so a blank one is
    // replaced with a visible placeholder to keep the field and the rest of the notification.
    public const string BLANK_FIELD_PLACEHOLDER = "-";

    public static WebhookEmbedAuthor StandardAuthor { get; } =
        new(
            Name: "FunFair BuildBot",
            Url: new("https://funfair.io"),
            IconUrl: new("https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png")
        );

    public static WebhookPayload ToPayload(DiscordEmbed embed)
    {
        return new([ToWebhookEmbed(embed)]);
    }

    public static WebhookEmbed ToWebhookEmbed(DiscordEmbed embed)
    {
        return EmbedLimiter.Apply(
            new(
                Title: NullIfBlank(embed.Title),
                Url: WebUrlOrNull(embed.Url),
                Description: NullIfBlank(embed.Description),
                Color: embed.Color?.Value,
                Author: StandardAuthor,
                Fields: [.. embed.Fields.Select(ToWebhookEmbedField)]
            )
        );
    }

    private static WebhookEmbedField ToWebhookEmbedField(DiscordEmbedField field)
    {
        return new(Name: PlaceholderIfBlank(field.Name), Value: PlaceholderIfBlank(field.Value), Inline: field.Inline);
    }

    // Discord rejects the whole message when an embed URL is not http(s); on Linux "/path" parses as a file URI.
    private static Uri? WebUrlOrNull(Uri? url)
    {
        return url is { IsAbsoluteUri: true } && IsWebScheme(url.Scheme) ? url : null;
    }

    private static bool IsWebScheme(string scheme)
    {
        return StringComparer.Ordinal.Equals(x: scheme, y: Uri.UriSchemeHttps)
            || StringComparer.Ordinal.Equals(x: scheme, y: Uri.UriSchemeHttp);
    }

    // Title and description are optional, so a blank one is omitted rather than sent as an empty string Discord may reject.
    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string PlaceholderIfBlank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? BLANK_FIELD_PLACEHOLDER : value;
    }
}
