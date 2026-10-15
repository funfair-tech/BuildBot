using BuildBot.GitHub.Filters;
using BuildBot.GitHub.Helpers;
using BuildBot.GitHub.Publishers;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.GitHub;

public static class GitHubSetup
{
    public static IServiceCollection AddGitHub(this IServiceCollection services, GitHubConfiguration gitHubConfig)
    {
        return services
            .AddSingleton(gitHubConfig)
            .AddSingleton<IWebhookSignatureVerifier, WebhookSignatureVerifier>()
            .AddSingleton<GitHubWebhookSignatureFilter>()
            .AddSingleton<GithubPingNotificationHandler>()
            .AddSingleton<GithubPushNotificationHandler>()
            .AddSingleton<GithubStatusNotificationHandler>();
    }
}
