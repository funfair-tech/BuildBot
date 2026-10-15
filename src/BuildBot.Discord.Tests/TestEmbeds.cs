using BuildBot.Discord.Models;

namespace BuildBot.Discord.Tests;

internal static class TestEmbeds
{
    public static DiscordEmbed WithTitle(string title)
    {
        return new(Title: title, Url: null, Description: null, Color: null, Fields: []);
    }
}
