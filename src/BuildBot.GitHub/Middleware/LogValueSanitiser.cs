using System;

namespace BuildBot.GitHub.Middleware;

public static class LogValueSanitiser
{
    public const int MAXIMUM_LENGTH = 256;

    private const char UNSAFE_CHARACTER_PLACEHOLDER = '?';
    private const string TRUNCATION_MARKER = "...";

    // Client-supplied text reaches plain-text log sinks, so control, line-separator and bidi characters could forge or visually
    // reorder log lines, and long values could flood them.
    public static string Sanitise(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int length = KeptLength(value);
        bool truncated = length < value.Length;

        if (!truncated && !ContainsUnsafeCharacter(value))
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
                    destination[index] = IsUnsafe(source[index]) ? UNSAFE_CHARACTER_PLACEHOLDER : source[index];
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

    // C0 and C1 controls, U+2028 to U+202E (line and paragraph separators, then the bidi embeddings and overrides) and
    // U+2066 to U+2069 (the bidi isolates). Searched vectorised rather than per character so ordinary values stay cheap.
    private static bool ContainsUnsafeCharacter(string value)
    {
        ReadOnlySpan<char> span = value.AsSpan();

        return span.ContainsAnyInRange(lowInclusive: '\u0000', highInclusive: '\u001f')
            || span.ContainsAnyInRange(lowInclusive: '\u007f', highInclusive: '\u009f')
            || span.ContainsAnyInRange(lowInclusive: '\u2028', highInclusive: '\u202e')
            || span.ContainsAnyInRange(lowInclusive: '\u2066', highInclusive: '\u2069');
    }

    // Must match the ranges ContainsUnsafeCharacter searches.
    private static bool IsUnsafe(char character)
    {
        return character
            is <= '\u001f'
                or >= '\u007f'
                and <= '\u009f'
                or >= '\u2028'
                and <= '\u202e'
                or >= '\u2066'
                and <= '\u2069';
    }
}
