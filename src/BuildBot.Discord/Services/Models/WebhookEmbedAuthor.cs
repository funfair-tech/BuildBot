using System;
using System.Diagnostics;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("{Name}: {Url} {IconUrl}")]
public sealed record WebhookEmbedAuthor(string Name, Uri Url, Uri IconUrl);
