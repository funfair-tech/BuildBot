using BuildBot.GitHub.Publishers;
using FunFair.Test.Common;
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
        return services.AddMockedService<IMediator>().AddGitHub();
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
