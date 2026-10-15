using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("{Title}: {Url} {Description} {Color}")]
public sealed record WebhookEmbed(
    string? Title,
    Uri? Url,
    string? Description,
    uint? Color,
    WebhookEmbedAuthor Author,
    IReadOnlyList<WebhookEmbedField> Fields
);
