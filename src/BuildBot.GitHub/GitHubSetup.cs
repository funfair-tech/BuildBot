using BuildBot.GitHub.Helpers;
using BuildBot.GitHub.Middleware;
using BuildBot.GitHub.Publishers;
using FunFair.Common.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.GitHub;

public static class GitHubSetup
{
    private const string WEBHOOK_SECRET_KEY = "GitHub:WebhookSecret";

    public static IServiceCollection AddGitHub(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddGitHub(new GitHubConfiguration(configuration[WEBHOOK_SECRET_KEY] ?? string.Empty));
    }

    public static IServiceCollection AddGitHub(this IServiceCollection services, GitHubConfiguration gitHubConfig)
    {
        return services
            .AddCommonWebServices()
            .AddSingleton(gitHubConfig)
            .AddSingleton<IWebhookSignatureVerifier, WebhookSignatureVerifier>()
            .AddSingleton<GitHubWebhookSignatureMiddleware>()
            .AddSingleton<GithubPingNotificationHandler>()
            .AddSingleton<GithubPushNotificationHandler>()
            .AddSingleton<GithubStatusNotificationHandler>();
    }
}
