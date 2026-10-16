using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

public sealed class DiscordWebhookClient : IDiscordWebhookClient
{
    public const string HttpClientName = "DiscordWebhook";

    public const int MaximumResponseBodyLength = 1024;

    public const string ResponseBodyUnavailable = "(response body unavailable)";

    // HttpClient.Timeout stops applying once headers arrive, so a stalled error body needs its own limit.
    public static readonly TimeSpan ResponseBodyReadTimeout = TimeSpan.FromSeconds(10);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;

    public DiscordWebhookClient(IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
    {
        this._httpClientFactory = httpClientFactory;
        this._timeProvider = timeProvider;
    }

    public async ValueTask<WebhookSendResult> SendAsync(
        Uri webhookUrl,
        WebhookPayload payload,
        CancellationToken cancellationToken
    )
    {
        HttpClient client = this._httpClientFactory.CreateClient(HttpClientName);

        using HttpRequestMessage request = new(method: HttpMethod.Post, requestUri: webhookUrl)
        {
            Content = JsonContent.Create(
                inputValue: payload,
                jsonTypeInfo: DiscordWebhookSerializationContext.Default.WebhookPayload
            ),
        };

        // Headers only, so an error body is never buffered in full; only a bounded prefix of it is read below.
        using HttpResponseMessage response = await client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            return new(Success: true, StatusCode: response.StatusCode, ResponseBody: string.Empty);
        }

        string responseBody = await this.ReadBoundedResponseBodyAsync(
            content: response.Content,
            cancellationToken: cancellationToken
        );

        return new(Success: false, StatusCode: response.StatusCode, ResponseBody: responseBody);
    }

    private async ValueTask<string> ReadBoundedResponseBodyAsync(
        HttpContent content,
        CancellationToken cancellationToken
    )
    {
        using CancellationTokenSource timeout = new(delay: ResponseBodyReadTimeout, timeProvider: this._timeProvider);
        using CancellationTokenSource readCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            token1: cancellationToken,
            token2: timeout.Token
        );

        // The status code is the primary diagnostic, so failing to read the body must not lose it.
        try
        {
            return await ReadResponseBodyPrefixAsync(content: content, cancellationToken: readCancellation.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            return ResponseBodyUnavailable;
        }
        catch (HttpRequestException)
        {
            return ResponseBodyUnavailable;
        }
        catch (IOException)
        {
            return ResponseBodyUnavailable;
        }
    }

    private static async ValueTask<string> ReadResponseBodyPrefixAsync(
        HttpContent content,
        CancellationToken cancellationToken
    )
    {
        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(stream);

        char[] buffer = new char[MaximumResponseBodyLength];
        int length = await reader.ReadBlockAsync(buffer: buffer.AsMemory(), cancellationToken: cancellationToken);

        return new(value: buffer, startIndex: 0, length: length);
    }
}
