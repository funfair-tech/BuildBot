using System;
using System.Collections.Generic;
using System.Linq;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class EmbedMapperTests : TestBase
{
    private const int MAX_TOTAL_LENGTH = 6000;

    [Fact]
    public void ToWebhookEmbed_MapsAllPropertiesAndAddsStandardAuthor()
    {
        DiscordEmbed embed = new(
            Title: "Title",
            Url: new("https://example.com/build"),
            Description: "Description",
            Color: DiscordColor.Red,
            Fields: [new(Name: "First", Value: "One"), new(Name: "Second", Value: "Two", Inline: true)]
        );

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Equal(expected: "Title", actual: result.Title);
        Assert.Equal(expected: new Uri("https://example.com/build"), actual: result.Url);
        Assert.Equal(expected: "Description", actual: result.Description);
        Assert.Equal(expected: 0xE74C3Cu, actual: result.Color);
        Assert.Same(expected: EmbedMapper.StandardAuthor, actual: result.Author);
        Assert.Equal(
            expected:
            [
                new WebhookEmbedField(Name: "First", Value: "One", Inline: false),
                new WebhookEmbedField(Name: "Second", Value: "Two", Inline: true),
            ],
            actual: result.Fields
        );
    }

    [Fact]
    public void ToWebhookEmbed_WithoutOptionalValues_LeavesThemNull()
    {
        DiscordEmbed embed = new(Title: string.Empty, Url: null, Description: string.Empty, Color: null, Fields: []);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Null(result.Title);
        Assert.Null(result.Url);
        Assert.Null(result.Description);
        Assert.Null(result.Color);
        Assert.Empty(result.Fields);
    }

    [Theory]
    [InlineData("https://example.com/build/1")]
    [InlineData("http://example.com/build/1")]
    public static void ToWebhookEmbed_WithWebUrl_KeepsUrl(string value)
    {
        Uri url = new(value);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(CreateEmbedWithUrl(url));

        Assert.Equal(expected: url, actual: result.Url);
    }

    [Theory]
    [InlineData("/builds/123")]
    [InlineData("builds/123")]
    [InlineData("ftp://example.com/build/1")]
    [InlineData("file:///builds/123")]
    public static void ToWebhookEmbed_WithRelativeOrNonWebUrl_DropsUrl(string value)
    {
        Uri url = new(uriString: value, uriKind: UriKind.RelativeOrAbsolute);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(CreateEmbedWithUrl(url));

        Assert.Null(result.Url);
    }

    [Fact]
    public void ToPayload_ContainsSingleMappedEmbed()
    {
        DiscordEmbed embed = new(Title: "Title", Url: null, Description: null, Color: DiscordColor.Green, Fields: []);

        WebhookPayload payload = EmbedMapper.ToPayload(embed);

        WebhookEmbed result = Assert.Single(payload.Embeds);
        Assert.Equal(expected: "Title", actual: result.Title);
        Assert.Equal(expected: 0x2ECC71u, actual: result.Color);
    }

    [Fact]
    public void StandardAuthor_IsFunFairBuildBot()
    {
        WebhookEmbedAuthor author = EmbedMapper.StandardAuthor;

        Assert.Equal(expected: "FunFair BuildBot", actual: author.Name);
        Assert.Equal(expected: new Uri("https://funfair.io"), actual: author.Url);
        Assert.Equal(
            expected: new Uri("https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png"),
            actual: author.IconUrl
        );
    }

    [Theory]
    [InlineData(256, 256)]
    [InlineData(257, 256)]
    [InlineData(1000, 256)]
    public static void ToWebhookEmbed_LimitsTitleLength(int length, int expectedLength)
    {
        DiscordEmbed embed = new(Title: Text(length), Url: null, Description: null, Color: null, Fields: []);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        AssertTruncatedTo(original: embed.Title, actual: result.Title, expectedLength: expectedLength);
    }

    [Theory]
    [InlineData(4096, 4096)]
    [InlineData(4097, 4096)]
    public static void ToWebhookEmbed_LimitsDescriptionLength(int length, int expectedLength)
    {
        DiscordEmbed embed = new(Title: string.Empty, Url: null, Description: Text(length), Color: null, Fields: []);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        AssertTruncatedTo(original: embed.Description, actual: result.Description, expectedLength: expectedLength);
    }

    [Theory]
    [InlineData(256, 256)]
    [InlineData(257, 256)]
    public static void ToWebhookEmbed_LimitsFieldNameLength(int length, int expectedLength)
    {
        DiscordEmbedField field = new(Name: Text(length), Value: "Value");

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(CreateEmbedWithFields([field]));

        WebhookEmbedField mapped = Assert.Single(result.Fields);
        AssertTruncatedTo(original: field.Name, actual: mapped.Name, expectedLength: expectedLength);
        Assert.Equal(expected: "Value", actual: mapped.Value);
    }

    [Theory]
    [InlineData(1024, 1024)]
    [InlineData(1025, 1024)]
    public static void ToWebhookEmbed_LimitsFieldValueLength(int length, int expectedLength)
    {
        DiscordEmbedField field = new(Name: "Name", Value: Text(length), Inline: true);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(CreateEmbedWithFields([field]));

        WebhookEmbedField mapped = Assert.Single(result.Fields);
        Assert.Equal(expected: "Name", actual: mapped.Name);
        AssertTruncatedTo(original: field.Value, actual: mapped.Value, expectedLength: expectedLength);
        Assert.True(mapped.Inline, userMessage: "Inline should be preserved");
    }

    [Theory]
    [InlineData(25, 25)]
    [InlineData(26, 25)]
    public static void ToWebhookEmbed_LimitsFieldCount(int count, int expectedCount)
    {
        DiscordEmbedField[] fields =
        [
            .. Enumerable
                .Range(start: 0, count: count)
                .Select(static index => new DiscordEmbedField(Name: $"Name{index}", Value: "Value")),
        ];

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(CreateEmbedWithFields(fields));

        Assert.Equal(expected: expectedCount, actual: result.Fields.Count);
        Assert.Equal(expected: "Name0", actual: result.Fields[0].Name);
        Assert.Equal(expected: "Name24", actual: result.Fields[^1].Name);
    }

    [Fact]
    public void ToWebhookEmbed_TruncationDoesNotSplitSurrogatePair()
    {
        string title = new string(c: 'a', count: 252) + "\U0001F600" + "bbb";
        DiscordEmbed embed = new(Title: title, Url: null, Description: null, Color: null, Fields: []);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Equal(expected: new string(c: 'a', count: 252) + "...", actual: result.Title);
    }

    [Fact]
    public void ToWebhookEmbed_AtTotalLimit_IsUnchanged()
    {
        DiscordEmbed embed = CreateEmbedAtTotalLength(lastValueLength: 252);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Equal(expected: MAX_TOTAL_LENGTH, actual: TotalLength(result));
        Assert.Equal(expected: embed.Description, actual: result.Description);
        Assert.Equal(expected: embed.Fields[^1].Value, actual: result.Fields[^1].Value);
    }

    [Fact]
    public void ToWebhookEmbed_OverTotalLimit_ShrinksDescriptionFirst()
    {
        DiscordEmbed embed = CreateEmbedAtTotalLength(lastValueLength: 253);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Equal(expected: MAX_TOTAL_LENGTH, actual: TotalLength(result));
        AssertTruncatedTo(original: embed.Description, actual: result.Description, expectedLength: 4095);
        Assert.Equal(expected: embed.Fields[^1].Value, actual: result.Fields[^1].Value);
    }

    [Fact]
    public void ToWebhookEmbed_OverTotalLimitWithoutDescription_ShrinksLastFieldValue()
    {
        // Title 256 + author 16 + 6 fields of (10 + 1024) = 6476, so the last value must lose 476 characters.
        DiscordEmbed embed = CreateEmbedWithFullValues(fieldCount: 6);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Equal(expected: MAX_TOTAL_LENGTH, actual: TotalLength(result));
        Assert.Equal(expected: 6, actual: result.Fields.Count);
        Assert.Equal(expected: embed.Fields[4].Value, actual: result.Fields[4].Value);
        AssertTruncatedTo(original: embed.Fields[5].Value, actual: result.Fields[5].Value, expectedLength: 548);
    }

    [Fact]
    public void ToWebhookEmbed_OverTotalLimitByMoreThanOneValue_ShrinksValuesFromTheLastFieldBack()
    {
        // Title 256 + author 16 + 7 fields of (10 + 1024) = 7510; the last value shrinks to 4 (-1020), the one before by 490.
        DiscordEmbed embed = CreateEmbedWithFullValues(fieldCount: 7);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Equal(expected: MAX_TOTAL_LENGTH, actual: TotalLength(result));
        Assert.Equal(expected: 7, actual: result.Fields.Count);
        Assert.Equal(expected: embed.Fields[4].Value, actual: result.Fields[4].Value);
        AssertTruncatedTo(original: embed.Fields[5].Value, actual: result.Fields[5].Value, expectedLength: 534);
        AssertTruncatedTo(original: embed.Fields[6].Value, actual: result.Fields[6].Value, expectedLength: 4);
    }

    [Fact]
    public void ToWebhookEmbed_FieldNamesAloneOverTotalLimit_DropsTrailingFields()
    {
        // Author 16 + description 2 + 25 fields of (256 + 10) = 6668; shrinking every value to 4 still leaves 6518, so only
        // the first 23 fields (23 x 260 = 5980 of the 5982 left) are kept.
        DiscordEmbedField[] fields =
        [
            .. Enumerable
                .Range(start: 0, count: 25)
                .Select(static _ => new DiscordEmbedField(Name: Text(256), Value: Text(10))),
        ];
        DiscordEmbed embed = new(Title: string.Empty, Url: null, Description: Text(2), Color: null, Fields: fields);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.True(
            TotalLength(result) <= MAX_TOTAL_LENGTH,
            userMessage: "Total length should be within Discord's limit"
        );
        Assert.Equal(expected: 23, actual: result.Fields.Count);
        Assert.All(
            collection: result.Fields,
            action: static field => Assert.Equal(expected: "a...", actual: field.Value)
        );
    }

    private static DiscordEmbed CreateEmbedWithUrl(Uri url)
    {
        return new(Title: "Title", Url: url, Description: null, Color: null, Fields: []);
    }

    private static DiscordEmbed CreateEmbedWithFields(IReadOnlyList<DiscordEmbedField> fields)
    {
        return new(Title: "Title", Url: null, Description: null, Color: null, Fields: fields);
    }

    // Title 256 + author 16 + description 4096 + (256 + 1024) + (100 + lastValueLength) = 5748 + lastValueLength.
    private static DiscordEmbed CreateEmbedAtTotalLength(int lastValueLength)
    {
        return new(
            Title: Text(256),
            Url: null,
            Description: Text(4096),
            Color: null,
            Fields: [new(Name: Text(256), Value: Text(1024)), new(Name: Text(100), Value: Text(lastValueLength))]
        );
    }

    private static DiscordEmbed CreateEmbedWithFullValues(int fieldCount)
    {
        DiscordEmbedField[] fields =
        [
            .. Enumerable
                .Range(start: 0, count: fieldCount)
                .Select(static _ => new DiscordEmbedField(Name: Text(10), Value: Text(1024))),
        ];

        return new(Title: Text(256), Url: null, Description: null, Color: null, Fields: fields);
    }

    private static string Text(int length)
    {
        return new(c: 'a', count: length);
    }

    private static int TotalLength(WebhookEmbed embed)
    {
        return (embed.Title?.Length ?? 0)
            + (embed.Description?.Length ?? 0)
            + embed.Author.Name.Length
            + embed.Fields.Sum(static field => field.Name.Length + field.Value.Length);
    }

    private static void AssertTruncatedTo(string? original, string? actual, int expectedLength)
    {
        Assert.NotNull(original);
        Assert.NotNull(actual);
        Assert.Equal(expected: expectedLength, actual: actual.Length);

        if (original.Length <= expectedLength)
        {
            Assert.Equal(expected: original, actual: actual);

            return;
        }

        Assert.EndsWith(expectedEndString: "...", actualString: actual, comparisonType: StringComparison.Ordinal);
        Assert.StartsWith(
            expectedStartString: original[..(expectedLength - 3)],
            actualString: actual,
            comparisonType: StringComparison.Ordinal
        );
    }
}
