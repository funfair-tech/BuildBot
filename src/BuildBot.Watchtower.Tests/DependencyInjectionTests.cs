using BuildBot.Watchtower.Publishers;
using FunFair.Test.Common;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.Watchtower.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddMockedService<IMediator>().AddWatchtower();
    }

    [Fact]
    public void WatchTowerPublishMessageNotificationHandlerMustBeRegistered()
    {
        this.RequireService<WatchTowerPublishMessageNotificationHandler>();
    }
}
