using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.Watchtower;

public static class WatchtowerSetup
{
    public static IServiceCollection AddWatchtower(this IServiceCollection services)
    {
        return services;
    }
}