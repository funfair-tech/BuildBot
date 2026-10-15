using System;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class EmbedMapperTests : TestBase
{
    [Fact]
    public void ToWebhookEmbed_MapsAllPropertiesAndAddsStandardAuthor()
    {
        DiscordEmbed embed = new(
            Title: "Title",
            Url: new("https://example.com/build"),
            Description: "Description",
            Colour: DiscordColour.Red,
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
        DiscordEmbed embed = new(Title: string.Empty, Url: null, Description: string.Empty, Colour: null, Fields: []);

        WebhookEmbed result = EmbedMapper.ToWebhookEmbed(embed);

        Assert.Null(result.Title);
        Assert.Null(result.Url);
        Assert.Null(result.Description);
        Assert.Null(result.Color);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public void ToPayload_ContainsSingleMappedEmbed()
    {
        DiscordEmbed embed = new(Title: "Title", Url: null, Description: null, Colour: DiscordColour.Green, Fields: []);

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
}
