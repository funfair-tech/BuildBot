using System.Diagnostics;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Name}: {Value} (Inline: {Inline})")]
public readonly record struct DiscordEmbedField(string Name, string Value, bool Inline = false);
