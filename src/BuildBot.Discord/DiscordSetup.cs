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
using Polly.Extensions.Http;

namespace BuildBot.Discord;

public static class DiscordSetup
{
    private const int MAX_RETRIES = 3;
    private static readonly TimeSpan MaximumRateLimitDelay = TimeSpan.FromSeconds(60);

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
        if (response?.StatusCode == HttpStatusCode.TooManyRequests)
        {
            TimeSpan? retryAfter = GetRetryAfter(retryAfter: response.Headers.RetryAfter, now: now);

            if (retryAfter is not null)
            {
                return Clamp(retryAfter.Value);
            }
        }

        return CalculateRetryDelay(attempt);
    }

    public static Task OnRetryAsync(
        DelegateResult<HttpResponseMessage> delegateResult,
        TimeSpan timeSpan,
        int attempt,
        Context context
    )
    {
        return Task.CompletedTask;
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

    private static TimeSpan Clamp(in TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return delay < MaximumRateLimitDelay ? delay : MaximumRateLimitDelay;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(TimeProvider timeProvider)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(static response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: MAX_RETRIES,
                sleepDurationProvider: (attempt, outcome, _) =>
                    CalculateRetryDelay(attempt: attempt, response: outcome.Result, now: timeProvider.GetUtcNow()),
                onRetryAsync: OnRetryAsync
            );
    }

    private static IServiceCollection AddDiscordWebhookHttpClient(this IServiceCollection services)
    {
        // The webhook URL embeds its token, so the default request logging (which writes the URL) is removed.
        return services
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .RemoveAllLoggers()
            .AddPolicyHandler(
                (serviceProvider, _) => CreateRetryPolicy(serviceProvider.GetRequiredService<TimeProvider>())
            )
            .Services;
    }
}
