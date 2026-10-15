using System;
using BuildBot.Discord.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Models;

public sealed class EmbedUrlTests : TestBase
{
    [Theory]
    [InlineData("https://example.com/build/1")]
    [InlineData("http://example.com/build/1")]
    public static void FromString_WithWebUrl_ReturnsUri(string value)
    {
        Uri? uri = EmbedUrl.FromString(value);

        Assert.Equal(expected: new Uri(value), actual: uri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/builds/123")]
    [InlineData("ftp://example.com/build/1")]
    [InlineData("file:///builds/123")]
    public static void FromString_WithMissingRelativeOrNonWebUrl_ReturnsNull(string? value)
    {
        Assert.Null(EmbedUrl.FromString(value));
    }
}
