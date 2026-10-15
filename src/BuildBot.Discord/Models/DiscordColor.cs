using System.Diagnostics;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Value}")]
public readonly record struct DiscordColor(uint Value)
{
    public static DiscordColor Green { get; } = new(0x2ECC71);

    public static DiscordColor Red { get; } = new(0xE74C3C);
}
