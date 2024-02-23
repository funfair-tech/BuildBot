using BuildBot.Discord.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.Discord;

public static class DiscordSetup
{
    public static IServiceCollection AddDiscord(this IServiceCollection services, DiscordBotConfiguration discordConfig)
    {
        return services.AddSingleton(discordConfig)
                       .AddSingleton<DiscordBot>()
                       .AddSingleton<IDiscordBot>(s => s.GetRequiredService<DiscordBot>())
                       .AddHostedService<BotService>();
    }
}