using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Title}: {Url} {Description} {Color}")]
public sealed record DiscordEmbed(
    string Title,
    Uri? Url,
    string? Description,
    DiscordColor? Color,
    IReadOnlyList<DiscordEmbedField> Fields
);
