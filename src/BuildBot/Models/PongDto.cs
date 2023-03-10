using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.Models;

/// <summary>
///     Returned after a successful call to PING!
/// </summary>
[DebuggerDisplay(value: "{Value}")]
public sealed class PongDto
{
    [JsonConstructor]
    public PongDto(string value)
    {
        this.Value = value;
    }

    /// <summary>
    ///     The value
    /// </summary>
    public string Value { get; }
}