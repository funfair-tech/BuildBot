using System;

namespace BuildBot.GitHub.Middleware;

public static class LogValueSanitiser
{
    public const int MAXIMUM_LENGTH = 256;

    private const char CONTROL_CHARACTER_PLACEHOLDER = '?';
    private const string TRUNCATION_MARKER = "...";

    // Client-supplied text reaches plain-text log sinks, so control characters could forge log lines and long values could flood them.
    public static string Sanitise(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int length = KeptLength(value);
        bool truncated = length < value.Length;

        if (!truncated && !ContainsControlCharacter(value))
        {
            return value;
        }

        return string.Create(
            length: truncated ? length + TRUNCATION_MARKER.Length : length,
            state: (value, length, truncated),
            action: static (destination, state) =>
            {
                ReadOnlySpan<char> source = state.value.AsSpan(start: 0, length: state.length);

                for (int index = 0; index < source.Length; index++)
                {
                    destination[index] = char.IsControl(source[index]) ? CONTROL_CHARACTER_PLACEHOLDER : source[index];
                }

                if (state.truncated)
                {
                    TRUNCATION_MARKER.AsSpan().CopyTo(destination[state.length..]);
                }
            }
        );
    }

    private static int KeptLength(string value)
    {
        if (value.Length <= MAXIMUM_LENGTH)
        {
            return value.Length;
        }

        // Cutting between the halves of a surrogate pair would leave an unpaired surrogate in the log.
        return char.IsHighSurrogate(value[MAXIMUM_LENGTH - 1]) ? MAXIMUM_LENGTH - 1 : MAXIMUM_LENGTH;
    }

    // These are exactly the ranges char.IsControl matches, searched vectorised rather than per character.
    private static bool ContainsControlCharacter(string value)
    {
        ReadOnlySpan<char> span = value.AsSpan();

        return span.ContainsAnyInRange(lowInclusive: '\u0000', highInclusive: '\u001f')
            || span.ContainsAnyInRange(lowInclusive: '\u007f', highInclusive: '\u009f');
    }
}
