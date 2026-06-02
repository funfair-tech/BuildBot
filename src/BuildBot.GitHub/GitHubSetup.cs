using BuildBot.GitHub.Publishers;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.GitHub;

public static class GitHubSetup
{
    public static IServiceCollection AddGitHub(this IServiceCollection services)
    {
        return services
            .AddSingleton<GithubPingNotificationHandler>()
            .AddSingleton<GithubPushNotificationHandler>()
            .AddSingleton<GithubStatusNotificationHandler>();
    }
}
