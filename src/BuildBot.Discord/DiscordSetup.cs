using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using BuildBot.ServiceModel.ComponentStatus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;

namespace BuildBot.Discord;

public static class DiscordSetup
{
    private const int MAX_RETRIES = 3;
    private static readonly TimeSpan MaximumRetryAfterDelay = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttemptAllowance = TimeSpan.FromSeconds(30);

    // HttpClient.Timeout wraps the whole policy pipeline, so it must cover every attempt and every wait between them.
    public static readonly TimeSpan WebhookTimeout =
        MaximumRetryAfterDelay * MAX_RETRIES + AttemptAllowance * (MAX_RETRIES + 1);

    public static IServiceCollection AddDiscord(this IServiceCollection services, DiscordBotConfiguration discordConfig)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddSingleton(discordConfig)
            .AddSingleton<IDiscordWebhookClient, DiscordWebhookClient>()
            .AddSingleton<DiscordBot>()
            .AddSingleton<IDiscordBot>(s => s.GetRequiredService<DiscordBot>())
            .AddSingleton<IComponentStatus>(s => s.GetRequiredService<DiscordBot>())
            .AddHostedService<DiscordMessageSender>()
            .AddSingleton(typeof(IMessageChannel<>), typeof(MessageChannel<>))
            .AddDiscordWebhookHttpClient();
    }

    public static TimeSpan CalculateRetryDelay(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Pow(x: 2, y: attempt));
    }

    public static TimeSpan CalculateRetryDelay(int attempt, HttpResponseMessage? response, in DateTimeOffset now)
    {
        TimeSpan? retryAfter = GetRetryAfter(retryAfter: response?.Headers.RetryAfter, now: now);

        if (retryAfter is null)
        {
            return CalculateRetryDelay(attempt);
        }

        return retryAfter.Value < TimeSpan.Zero ? TimeSpan.Zero : retryAfter.Value;
    }

    public static bool ShouldRetry(HttpResponseMessage response, in DateTimeOffset now)
    {
        if (!IsTransientFailure(response.StatusCode))
        {
            return false;
        }

        // A server asking for a longer wait than we are prepared to give is not retried; the response is returned as-is.
        TimeSpan? retryAfter = GetRetryAfter(retryAfter: response.Headers.RetryAfter, now: now);

        return retryAfter is null || retryAfter.Value <= MaximumRetryAfterDelay;
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
    {
        return statusCode
            is >= HttpStatusCode.InternalServerError
                or HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests;
    }

    private static TimeSpan? GetRetryAfter(RetryConditionHeaderValue? retryAfter, in DateTimeOffset now)
    {
        if (retryAfter is null)
        {
            return null;
        }

        // The header always carries exactly one of a delta or a date.
        return retryAfter.Delta ?? retryAfter.Date.GetValueOrDefault(now) - now;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(TimeProvider timeProvider)
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => ShouldRetry(response: response, now: timeProvider.GetUtcNow()))
            .WaitAndRetryAsync(
                retryCount: MAX_RETRIES,
                sleepDurationProvider: (attempt, outcome, _) =>
                    CalculateRetryDelay(attempt: attempt, response: outcome.Result, now: timeProvider.GetUtcNow()),
                onRetryAsync: static (_, _, _, _) => Task.CompletedTask
            );
    }

    private static IServiceCollection AddDiscordWebhookHttpClient(this IServiceCollection services)
    {
        // The webhook URL embeds its token, so the default request logging (which writes the URL) is removed.
        return services
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .RemoveAllLoggers()
            .ConfigureHttpClient(static client => client.Timeout = WebhookTimeout)
            .AddPolicyHandler(
                (serviceProvider, _) => CreateRetryPolicy(serviceProvider.GetRequiredService<TimeProvider>())
            )
            .Services;
    }
}
