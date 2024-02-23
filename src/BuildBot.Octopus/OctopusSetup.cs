using Microsoft.Extensions.DependencyInjection;
using Octopus.Client;

namespace BuildBot.Octopus;

public static class OctopusSetup
{
    public static IServiceCollection AddOctopus(this IServiceCollection services, OctopusServerEndpoint octopusServerEndpoint)
    {
        return services.AddSingleton<IOctopusClientFactory, OctopusClientFactory>()
                       .AddSingleton(octopusServerEndpoint);
    }
}