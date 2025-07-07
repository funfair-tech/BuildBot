using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.GitHub.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddGitHub();
    }

    [Fact]
    public static void ShouldUseGitHub()
    {
        Assert.True(condition: true, userMessage: "Placeholder");
    }
}
