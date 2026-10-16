using System;
using System.Linq;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

// Discord rejects the whole message when any embed limit is exceeded, so oversized text is cut down rather than lost entirely.
internal static class EmbedLimiter
{
    public const int MAX_TITLE_LENGTH = 256;
    public const int MAX_DESCRIPTION_LENGTH = 4096;
    public const int MAX_FIELD_NAME_LENGTH = 256;
    public const int MAX_FIELD_VALUE_LENGTH = 1024;
    public const int MAX_FIELDS = 25;
    public const int MAX_TOTAL_LENGTH = 6000;

    public const string TRUNCATION_MARKER = "...";

    // Room for one character of the original text in front of the three-character marker when shrinking to fit the total.
    private const int MIN_SHRUNK_LENGTH = 4;

    public static WebhookEmbed Apply(WebhookEmbed embed)
    {
        WebhookEmbed limited = embed with
        {
            Title = TruncateOrNull(value: embed.Title, maxLength: MAX_TITLE_LENGTH),
            Description = TruncateOrNull(value: embed.Description, maxLength: MAX_DESCRIPTION_LENGTH),
            Fields = [.. embed.Fields.Take(MAX_FIELDS).Select(LimitField)],
        };

        return FitTotalLength(limited);
    }

    private static int TotalLength(WebhookEmbed embed)
    {
        return FixedLength(embed) + embed.Fields.Sum(FieldLength);
    }

    private static WebhookEmbedField LimitField(WebhookEmbedField field)
    {
        return field with
        {
            Name = Truncate(value: field.Name, maxLength: MAX_FIELD_NAME_LENGTH),
            Value = Truncate(value: field.Value, maxLength: MAX_FIELD_VALUE_LENGTH),
        };
    }

    // Shrinks the least important text first: the description, then field values from the last field back, and only drops
    // trailing fields when the field names alone are still too long.
    private static WebhookEmbed FitTotalLength(WebhookEmbed embed)
    {
        if (TotalLength(embed) <= MAX_TOTAL_LENGTH)
        {
            return embed;
        }

        WebhookEmbed shrunkDescription = ShrinkDescription(embed: embed, excess: TotalLength(embed) - MAX_TOTAL_LENGTH);

        if (TotalLength(shrunkDescription) <= MAX_TOTAL_LENGTH)
        {
            return shrunkDescription;
        }

        WebhookEmbed shrunkValues = ShrinkFieldValues(
            embed: shrunkDescription,
            excess: TotalLength(shrunkDescription) - MAX_TOTAL_LENGTH
        );

        return TotalLength(shrunkValues) <= MAX_TOTAL_LENGTH ? shrunkValues : DropTrailingFields(shrunkValues);
    }

    private static WebhookEmbed ShrinkDescription(WebhookEmbed embed, int excess)
    {
        return embed.Description is null
            ? embed
            : embed with
            {
                Description = Shrink(value: embed.Description, excess: excess),
            };
    }

    private static WebhookEmbed ShrinkFieldValues(WebhookEmbed embed, int excess)
    {
        WebhookEmbedField[] fields = [.. embed.Fields];
        int remainingExcess = excess;

        for (int index = fields.Length - 1; index >= 0 && remainingExcess > 0; index--)
        {
            string shrunk = Shrink(value: fields[index].Value, excess: remainingExcess);
            remainingExcess -= fields[index].Value.Length - shrunk.Length;
            fields[index] = fields[index] with { Value = shrunk };
        }

        return embed with
        {
            Fields = fields,
        };
    }

    private static WebhookEmbed DropTrailingFields(WebhookEmbed embed)
    {
        int remaining = MAX_TOTAL_LENGTH - FixedLength(embed);
        int kept = 0;

        while (kept < embed.Fields.Count && FieldLength(embed.Fields[kept]) <= remaining)
        {
            remaining -= FieldLength(embed.Fields[kept]);
            kept++;
        }

        return embed with
        {
            Fields = [.. embed.Fields.Take(kept)],
        };
    }

    private static int FixedLength(WebhookEmbed embed)
    {
        return (embed.Title?.Length ?? 0) + (embed.Description?.Length ?? 0) + embed.Author.Name.Length;
    }

    private static int FieldLength(WebhookEmbedField field)
    {
        return field.Name.Length + field.Value.Length;
    }

    private static string Shrink(string value, int excess)
    {
        return Truncate(value: value, maxLength: Math.Max(val1: value.Length - excess, val2: MIN_SHRUNK_LENGTH));
    }

    private static string? TruncateOrNull(string? value, int maxLength)
    {
        return value is null ? null : Truncate(value: value, maxLength: maxLength);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        int kept = maxLength - TRUNCATION_MARKER.Length;

        // Cutting between the halves of a surrogate pair would leave an unpaired surrogate, which is not valid UTF-16.
        if (char.IsHighSurrogate(value[kept - 1]))
        {
            kept--;
        }

        return string.Concat(str0: value.AsSpan(start: 0, length: kept), str1: TRUNCATION_MARKER);
    }
}
