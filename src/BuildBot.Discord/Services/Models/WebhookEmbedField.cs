using System.Diagnostics;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("{Name}: {Value} (Inline: {Inline})")]
public readonly record struct WebhookEmbedField(string Name, string Value, bool Inline);
