using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.Models;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordWebhookClientTests : TestBase
{
    private static readonly Uri WebhookUrl = new("https://example.com/webhook/secret-token");

    private static readonly WebhookPayload Payload = new([
        new WebhookEmbed(
            Title: "Title",
            Url: new("https://example.com/build"),
            Description: null,
            Color: 0x2ECC71,
            Author: new(Name: "Author", Url: new("https://example.com/"), IconUrl: new("https://example.com/icon.png")),
            Fields: [new(Name: "Name", Value: "Value", Inline: true)]
        ),
    ]);

    private const string EXPECTED_JSON =
        """{"embeds":[{"title":"Title","url":"https://example.com/build","color":3066993,"author":{"name":"Author","url":"https://example.com/","icon_url":"https://example.com/icon.png"},"fields":[{"name":"Name","value":"Value","inline":true}]}]}""";

    private static DiscordWebhookClient CreateClient(TestHttpMessageHandler handler)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory
            .CreateClient(DiscordWebhookClient.HttpClientName)
            .Returns(_ => new HttpClient(handler: handler, disposeHandler: false));

        return new(factory);
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode statusCode)
    {
        return () => new(statusCode);
    }

    [Fact]
    public async Task SendAsync_PostsPayloadAsJsonToWebhookUrl()
    {
        using TestHttpMessageHandler handler = new(Respond(HttpStatusCode.NoContent));
        DiscordWebhookClient client = CreateClient(handler);

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: new(Success: true, StatusCode: HttpStatusCode.NoContent), actual: result);

        TestHttpMessageHandler.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(expected: HttpMethod.Post, actual: request.Method);
        Assert.Equal(expected: WebhookUrl, actual: request.RequestUri);
        Assert.Equal(expected: "application/json", actual: request.ContentType);
        Assert.Equal(expected: EXPECTED_JSON, actual: request.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.NoContent, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task SendAsync_ReturnsResultForStatusWithoutThrowingAsync(
        HttpStatusCode statusCode,
        bool expectedSuccess
    )
    {
        using TestHttpMessageHandler handler = new(Respond(statusCode));
        DiscordWebhookClient client = CreateClient(handler);

        WebhookSendResult result = await client.SendAsync(
            webhookUrl: WebhookUrl,
            payload: Payload,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: new(Success: expectedSuccess, StatusCode: statusCode), actual: result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_Throws()
    {
        using TestHttpMessageHandler handler = new(Respond(HttpStatusCode.NoContent));
        DiscordWebhookClient client = CreateClient(handler);

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.SendAsync(webhookUrl: WebhookUrl, payload: Payload, cancellationToken: cts.Token)
        );
    }
}
