using System.Diagnostics;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("Retry after: {RetryAfter}s, Global: {Global}")]
public sealed record RateLimitResponse(double? RetryAfter, bool Global);
