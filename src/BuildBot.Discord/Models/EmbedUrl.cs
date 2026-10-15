using System;

namespace BuildBot.Discord.Models;

public static class EmbedUrl
{
    public static Uri? FromString(string? value)
    {
        return Uri.TryCreate(uriString: value, uriKind: UriKind.Absolute, out Uri? uri) ? uri : null;
    }
}
