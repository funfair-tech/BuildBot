using BuildBot.Discord.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.Discord;

public static class DiscordSetup
{
    public static IServiceCollection AddDiscord(this IServiceCollection services, DiscordBotConfiguration discordConfig)
    {
        return services.AddSingleton(discordConfig)
                       .AddSingleton<IDiscordBot, DiscordBot>()
                       .AddHostedService<BotService>();
    }
}