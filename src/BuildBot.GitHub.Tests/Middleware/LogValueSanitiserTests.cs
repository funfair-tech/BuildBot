using BuildBot.GitHub.Middleware;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Middleware;

public sealed class LogValueSanitiserTests : TestBase
{
    [Fact]
    public void OrdinaryValueIsUnchanged()
    {
        const string value = "203.0.113.7, 198.51.100.2";

        string sanitised = LogValueSanitiser.Sanitise(value);

        Assert.Same(expected: value, actual: sanitised);
    }

    [Fact]
    public void EmptyValueIsUnchanged()
    {
        Assert.Equal(expected: string.Empty, actual: LogValueSanitiser.Sanitise(string.Empty));
    }

    [Fact]
    public void NullValueIsLoggedAsEmpty()
    {
        Assert.Equal(expected: string.Empty, actual: LogValueSanitiser.Sanitise(null));
    }

    [Fact]
    public void LineBreaksAreReplacedSoLogLinesCannotBeForged()
    {
        string sanitised = LogValueSanitiser.Sanitise("push\r\nGithub: Accepted webhook to /github");

        Assert.Equal(expected: "push??Github: Accepted webhook to /github", actual: sanitised);
    }

    [Fact]
    public void OtherControlCharactersAreReplaced()
    {
        string sanitised = LogValueSanitiser.Sanitise("\u001b[31mred\u001b[0m\t\0");

        Assert.Equal(expected: "?[31mred?[0m??", actual: sanitised);
    }

    [Theory]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [InlineData("\u202e")]
    public static void LineBreakAndBidiCharactersAreReplaced(string unsafeCharacter)
    {
        string sanitised = LogValueSanitiser.Sanitise("before" + unsafeCharacter + "after");

        Assert.Equal(expected: "before?after", actual: sanitised);
    }

    [Fact]
    public void CharactersNextToUnsafeRangesAreUnchanged()
    {
        const string value = "\u00a0\u2027\u202f\u2065\u206a";

        Assert.Same(expected: value, actual: LogValueSanitiser.Sanitise(value));
    }

    [Fact]
    public void CharactersNextToUnsafeRangesAreKeptWhenOtherCharactersAreReplaced()
    {
        string sanitised = LogValueSanitiser.Sanitise("\n\u00a0\u2027\u202f\u2065\u206a\u2066");

        Assert.Equal(expected: "?\u00a0\u2027\u202f\u2065\u206a?", actual: sanitised);
    }

    [Fact]
    public void ValueAtMaximumLengthIsUnchanged()
    {
        string value = new('a', count: LogValueSanitiser.MAXIMUM_LENGTH);

        Assert.Same(expected: value, actual: LogValueSanitiser.Sanitise(value));
    }

    [Fact]
    public void OverLongValueIsTruncatedWithMarker()
    {
        string value = new('a', count: LogValueSanitiser.MAXIMUM_LENGTH * 40);

        string sanitised = LogValueSanitiser.Sanitise(value);

        Assert.Equal(expected: new string('a', count: LogValueSanitiser.MAXIMUM_LENGTH) + "...", actual: sanitised);
    }

    [Fact]
    public void OverLongValueWithControlCharactersIsTruncatedAndReplaced()
    {
        string value = "a\nb" + new string('c', count: LogValueSanitiser.MAXIMUM_LENGTH);

        string sanitised = LogValueSanitiser.Sanitise(value);

        Assert.Equal(
            expected: "a?b" + new string('c', count: LogValueSanitiser.MAXIMUM_LENGTH - 3) + "...",
            actual: sanitised
        );
    }

    [Fact]
    public void TruncationDoesNotSplitSurrogatePair()
    {
        string value = new string('a', count: LogValueSanitiser.MAXIMUM_LENGTH - 1) + "\U0001F600" + "tail";

        string sanitised = LogValueSanitiser.Sanitise(value);

        Assert.Equal(expected: new string('a', count: LogValueSanitiser.MAXIMUM_LENGTH - 1) + "...", actual: sanitised);
    }
}
