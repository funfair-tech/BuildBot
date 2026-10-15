using System;
using BuildBot.Discord.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Models;

public sealed class EmbedUrlTests : TestBase
{
    [Fact]
    public void FromString_WithAbsoluteUrl_ReturnsUri()
    {
        Uri? uri = EmbedUrl.FromString("https://example.com/build/1");

        Assert.Equal(expected: new Uri("https://example.com/build/1"), actual: uri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    public static void FromString_WithMissingOrRelativeUrl_ReturnsNull(string? value)
    {
        Assert.Null(EmbedUrl.FromString(value));
    }
}
