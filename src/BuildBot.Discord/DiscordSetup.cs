using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using BuildBot.Discord.Services.LoggingExtensions;
using BuildBot.ServiceModel.ComponentStatus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace BuildBot.Discord;

public static class DiscordSetup
{
    private const int MAX_RETRIES = 3;
    private static readonly TimeSpan MaximumRetryAfterDelay = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttemptAllowance = TimeSpan.FromSeconds(30);

    // Held as a field so the delegate is created once, rather than via a lazily-cached method-group conversion.
    private static readonly Func<int, TimeSpan> ExponentialBackoff = CalculateRetryDelay;

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
        return CalculateRetryDelay(attempt: attempt, response: response, now: now, backoff: ExponentialBackoff);
    }

    private static TimeSpan CalculateRetryDelay(
        int attempt,
        HttpResponseMessage? response,
        in DateTimeOffset now,
        Func<int, TimeSpan> backoff
    )
    {
        TimeSpan? retryAfter = GetRetryAfter(retryAfter: response?.Headers.RetryAfter, now: now);

        if (retryAfter is null)
        {
            return backoff(attempt);
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

    public static IAsyncPolicy<HttpResponseMessage> CreateWebhookPolicy(
        TimeProvider timeProvider,
        in TimeSpan attemptTimeout,
        Func<int, TimeSpan> backoff,
        ILogger<DiscordWebhookClient> logger
    )
    {
        IAsyncPolicy<HttpResponseMessage> retry = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(response => ShouldRetry(response: response, now: timeProvider.GetUtcNow()))
            .WaitAndRetryAsync(
                retryCount: MAX_RETRIES,
                sleepDurationProvider: (attempt, outcome, _) =>
                    CalculateRetryDelay(
                        attempt: attempt,
                        response: outcome.Result,
                        now: timeProvider.GetUtcNow(),
                        backoff: backoff
                    ),
                onRetryAsync: (outcome, delay, attempt, _) =>
                    LogRetryAsync(outcome: outcome, attempt: attempt, delay: delay, logger: logger)
            );

        // Each attempt gets its own allowance inside the retry, so one stalled attempt is retried rather than
        // consuming the whole HttpClient.Timeout; the caller's own cancellation still surfaces as cancellation.
        IAsyncPolicy<HttpResponseMessage> attemptTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            attemptTimeout
        );

        return Policy.WrapAsync(retry, attemptTimeoutPolicy);
    }

    private static Task LogRetryAsync(
        DelegateResult<HttpResponseMessage> outcome,
        int attempt,
        in TimeSpan delay,
        ILogger<DiscordWebhookClient> logger
    )
    {
        if (outcome.Result is null)
        {
            logger.LogRetryingAfterException(
                attempt: attempt,
                exceptionType: outcome.Exception.GetType().Name,
                delay: delay
            );

            return Task.CompletedTask;
        }

        logger.LogRetryingAfterStatusCode(attempt: attempt, statusCode: outcome.Result.StatusCode, delay: delay);

        return Task.CompletedTask;
    }

    private static IServiceCollection AddDiscordWebhookHttpClient(this IServiceCollection services)
    {
        // The webhook URL embeds its token, so the default request logging (which writes the URL) is removed.
        return services
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .RemoveAllLoggers()
            .ConfigureHttpClient(static client => client.Timeout = WebhookTimeout)
            .AddPolicyHandler(
                (serviceProvider, _) =>
                    CreateWebhookPolicy(
                        timeProvider: serviceProvider.GetRequiredService<TimeProvider>(),
                        attemptTimeout: AttemptAllowance,
                        backoff: ExponentialBackoff,
                        logger: serviceProvider.GetRequiredService<ILogger<DiscordWebhookClient>>()
                    )
            )
            .Services;
    }
}
