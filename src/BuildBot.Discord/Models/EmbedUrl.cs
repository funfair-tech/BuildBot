using System;

namespace BuildBot.Discord.Models;

public static class EmbedUrl
{
    public static Uri? FromString(string? value)
    {
        return Uri.TryCreate(uriString: value, uriKind: UriKind.Absolute, out Uri? uri) && IsWebUrl(uri) ? uri : null;
    }

    // Discord rejects the whole message when an embed URL is not http(s); on Linux "/path" parses as a file URI.
    private static bool IsWebUrl(Uri uri)
    {
        return StringComparer.Ordinal.Equals(x: uri.Scheme, y: Uri.UriSchemeHttps)
            || StringComparer.Ordinal.Equals(x: uri.Scheme, y: Uri.UriSchemeHttp);
    }
}
