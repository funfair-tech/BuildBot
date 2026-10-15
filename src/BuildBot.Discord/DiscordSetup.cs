using System;
using BuildBot.Discord.Services;
using BuildBot.ServiceModel.ComponentStatus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;

namespace BuildBot.Discord;

public static class DiscordSetup
{
    private const int MAX_RETRIES = 3;

    public static IServiceCollection AddDiscord(this IServiceCollection services, DiscordBotConfiguration discordConfig)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddSingleton(discordConfig)
            .AddSingleton<IDiscordWebhookClient, DiscordWebhookClient>()
            .AddSingleton<DiscordBot>()
            .AddSingleton<IDiscordBot>(s => s.GetRequiredService<DiscordBot>())
            .AddSingleton<IComponentStatus>(s => s.GetRequiredService<DiscordBot>())
            .AddHostedService<BotService>()
            .AddSingleton(typeof(IMessageChannel<>), typeof(MessageChannel<>))
            .AddDiscordWebhookHttpClient();
    }

    public static TimeSpan CalculateRetryDelay(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Pow(x: 2, y: attempt));
    }

    private static IServiceCollection AddDiscordWebhookHttpClient(this IServiceCollection services)
    {
        // The webhook URL embeds its token, so the default request logging (which writes the URL) is removed.
        return services
            .AddHttpClient(DiscordWebhookClient.HttpClientName)
            .RemoveAllLoggers()
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(retryCount: MAX_RETRIES, sleepDurationProvider: CalculateRetryDelay)
            )
            .Services;
    }
}
