using BuildBot.Health.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.Health;

public static class StatusSetup
{
    public static IServiceCollection AddStatus(this IServiceCollection services)
    {
        return services.AddSingleton<IServerStatus, ServerStatus>();
    }
}
