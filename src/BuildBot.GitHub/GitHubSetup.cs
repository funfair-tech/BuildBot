using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.GitHub;

public static class GitHubSetup
{
    public static IServiceCollection AddGitHub(this IServiceCollection services)
    {
        return services;
    }
}