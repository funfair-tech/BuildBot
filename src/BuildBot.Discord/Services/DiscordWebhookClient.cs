using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

public sealed class DiscordWebhookClient : IDiscordWebhookClient
{
    public const string HttpClientName = "DiscordWebhook";

    private readonly IHttpClientFactory _httpClientFactory;

    public DiscordWebhookClient(IHttpClientFactory httpClientFactory)
    {
        this._httpClientFactory = httpClientFactory;
    }

    public async ValueTask<WebhookSendResult> SendAsync(
        Uri webhookUrl,
        WebhookPayload payload,
        CancellationToken cancellationToken
    )
    {
        HttpClient client = this._httpClientFactory.CreateClient(HttpClientName);

        using JsonContent content = JsonContent.Create(
            inputValue: payload,
            jsonTypeInfo: DiscordWebhookSerializationContext.Default.WebhookPayload
        );

        using HttpResponseMessage response = await client.PostAsync(
            requestUri: webhookUrl,
            content: content,
            cancellationToken: cancellationToken
        );

        return new(Success: response.IsSuccessStatusCode, StatusCode: response.StatusCode);
    }
}
