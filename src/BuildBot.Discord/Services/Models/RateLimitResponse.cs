using System.Diagnostics;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("Retry after: {RetryAfter}s")]
public sealed record RateLimitResponse(double? RetryAfter);
