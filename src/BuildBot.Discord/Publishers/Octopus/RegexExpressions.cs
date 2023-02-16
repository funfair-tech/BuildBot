using System.Text.RegularExpressions;

namespace BuildBot.Discord.Publishers.Octopus;

internal static partial class RegexExpressions
{
    [GeneratedRegex(pattern: "(ff\\-\\d+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 5000)]
    public static partial Regex BuildNumber();
}