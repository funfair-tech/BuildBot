using System.Linq;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

public static class EmbedMapper
{
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
        return new(
            Title: NullIfEmpty(embed.Title),
            Url: embed.Url,
            Description: NullIfEmpty(embed.Description),
            Color: embed.Color?.Value,
            Author: StandardAuthor,
            Fields: [.. embed.Fields.Select(ToWebhookEmbedField)]
        );
    }

    private static WebhookEmbedField ToWebhookEmbedField(DiscordEmbedField field)
    {
        return new(Name: field.Name, Value: field.Value, Inline: field.Inline);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
