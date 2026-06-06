using BuildBot.Health;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.Health.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddStatus();
    }

    [Fact]
    public void ServerStatusMustBeRegistered()
    {
        this.RequireService<IServerStatus>();
    }
}
