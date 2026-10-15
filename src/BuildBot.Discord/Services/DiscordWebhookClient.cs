using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services.LoggingExtensions;
using BuildBot.Discord.Services.Models;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services;

public sealed class DiscordWebhookClient : IDiscordWebhookClient
{
    public const string HttpClientName = "DiscordWebhook";

    private const int MAX_ATTEMPTS = 3;
    private static readonly TimeSpan DefaultRateLimitDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumRateLimitDelay = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordWebhookClient> _logger;
    private readonly TimeProvider _timeProvider;

    public DiscordWebhookClient(
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider,
        ILogger<DiscordWebhookClient> logger
    )
    {
        this._httpClientFactory = httpClientFactory;
        this._timeProvider = timeProvider;
        this._logger = logger;
    }

    public async ValueTask<WebhookSendResult> SendAsync(
        Uri webhookUrl,
        WebhookPayload payload,
        CancellationToken cancellationToken
    )
    {
        HttpClient client = this._httpClientFactory.CreateClient(HttpClientName);

        int attempt = 1;

        while (true)
        {
            using HttpResponseMessage response = await PostAsync(
                client: client,
                webhookUrl: webhookUrl,
                payload: payload,
                cancellationToken: cancellationToken
            );

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt == MAX_ATTEMPTS)
            {
                return ToResult(response);
            }

            TimeSpan delay = await GetRateLimitDelayAsync(response: response, cancellationToken: cancellationToken);

            this._logger.LogRateLimited(attempt: attempt, delaySeconds: delay.TotalSeconds);

            await Task.Delay(delay: delay, timeProvider: this._timeProvider, cancellationToken: cancellationToken);

            ++attempt;
        }
    }

    public static TimeSpan CalculateRateLimitDelay(double? retryAfterSeconds, TimeSpan? retryAfterHeader)
    {
        if (retryAfterSeconds is > 0)
        {
            return TimeSpan.FromSeconds(
                Math.Min(val1: retryAfterSeconds.Value, val2: MaximumRateLimitDelay.TotalSeconds)
            );
        }

        if (retryAfterHeader is { } header && header > TimeSpan.Zero)
        {
            return header < MaximumRateLimitDelay ? header : MaximumRateLimitDelay;
        }

        return DefaultRateLimitDelay;
    }

    private static WebhookSendResult ToResult(HttpResponseMessage response)
    {
        return new(Success: response.IsSuccessStatusCode, StatusCode: response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        Uri webhookUrl,
        WebhookPayload payload,
        CancellationToken cancellationToken
    )
    {
        using JsonContent content = JsonContent.Create(
            inputValue: payload,
            jsonTypeInfo: DiscordWebhookSerializationContext.Default.WebhookPayload
        );

        return await client.PostAsync(requestUri: webhookUrl, content: content, cancellationToken: cancellationToken);
    }

    private static async ValueTask<TimeSpan> GetRateLimitDelayAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        RateLimitResponse? rateLimit = await ReadRateLimitResponseAsync(
            response: response,
            cancellationToken: cancellationToken
        );

        return CalculateRateLimitDelay(
            retryAfterSeconds: rateLimit?.RetryAfter,
            retryAfterHeader: response.Headers.RetryAfter?.Delta
        );
    }

    private static async ValueTask<RateLimitResponse?> ReadRateLimitResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await response.Content.ReadFromJsonAsync(
                jsonTypeInfo: DiscordWebhookSerializationContext.Default.RateLimitResponse,
                cancellationToken: cancellationToken
            );
        }
        catch (JsonException)
        {
            // Rate limits applied at Discord's edge return HTML rather than JSON; fall back to the header.
            return null;
        }
    }
}
