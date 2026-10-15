using BuildBot.GitHub.Filters;
using BuildBot.GitHub.Publishers;
using FunFair.Common.Web.Interfaces;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.GitHub.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddMockedService<IMediator>().AddGitHub(new GitHubConfiguration("secret"));
    }

    [Fact]
    public void GitHubConfigurationMustBeRegistered()
    {
        this.RequireService<GitHubConfiguration>();
    }

    [Fact]
    public void WebhookSignatureVerifierMustBeRegistered()
    {
        this.RequireService<IWebhookSignatureVerifier>();
    }

    [Fact]
    public void RemoteIpAddressRetrieverMustBeRegistered()
    {
        this.RequireService<IRemoteIpAddressRetriever>();
    }

    [Fact]
    public void GitHubWebhookSignatureFilterMustBeRegistered()
    {
        this.RequireService<GitHubWebhookSignatureFilter>();
    }

    [Fact]
    public void GithubPingNotificationHandlerMustBeRegistered()
    {
        this.RequireService<GithubPingNotificationHandler>();
    }

    [Fact]
    public void GithubPushNotificationHandlerMustBeRegistered()
    {
        this.RequireService<GithubPushNotificationHandler>();
    }

    [Fact]
    public void GithubStatusNotificationHandlerMustBeRegistered()
    {
        this.RequireService<GithubStatusNotificationHandler>();
    }
}
