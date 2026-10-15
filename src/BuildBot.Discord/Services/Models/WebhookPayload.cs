using System.Collections.Generic;
using System.Diagnostics;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("Embeds: {Embeds.Count}")]
public sealed record WebhookPayload(IReadOnlyList<WebhookEmbed> Embeds);
