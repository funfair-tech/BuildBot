using System.Diagnostics;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Value}")]
public readonly record struct DiscordColour(uint Value)
{
    public static DiscordColour Green { get; } = new(0x2ECC71);

    public static DiscordColour Red { get; } = new(0xE74C3C);
}
