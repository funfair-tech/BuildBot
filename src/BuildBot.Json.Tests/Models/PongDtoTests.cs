using BuildBot.Json.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Json.Tests.Models;

public sealed class PongDtoTests : TestBase
{
    [Fact]
    public void ValueShouldMatchWhatWasPassedToConstructor()
    {
        const string expected = "pong";
        PongDto pong = new(expected);

        Assert.Equal(expected: expected, actual: pong.Value);
    }

    [Fact]
    public void TwoDtosWithSameValueAreEqual()
    {
        const string value = "pong";
        PongDto first = new(value);
        PongDto second = new(value);

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoDtosWithDifferentValuesAreNotEqual()
    {
        PongDto first = new("pong");
        PongDto second = new("other");

        Assert.NotEqual(expected: first, actual: second);
    }
}
