using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Title}: {Url} {Description} {Colour}")]
public sealed record DiscordEmbed(
    string Title,
    Uri? Url,
    string? Description,
    DiscordColour? Colour,
    IReadOnlyList<DiscordEmbedField> Fields
);
