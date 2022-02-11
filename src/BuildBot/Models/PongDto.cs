using System.Diagnostics;

namespace BuildBot.Models;

/// <summary>
///     Returned after a successful call to PING!
/// </summary>
[DebuggerDisplay(value: "{Value}")]
public sealed class PongDto
{
    /// <summary>
    ///     The value
    /// </summary>
    public string Value { get; init; } = default!;
}