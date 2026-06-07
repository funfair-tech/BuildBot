using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class PusherTests : TestBase
{
    [Fact]
    public void NamePropertyMatchesConstructorArgument()
    {
        Pusher pusher = new(name: "deployer", email: "deploy@example.com");

        Assert.Equal(expected: "deployer", actual: pusher.Name);
    }

    [Fact]
    public void EmailPropertyMatchesConstructorArgument()
    {
        Pusher pusher = new(name: "deployer", email: "deploy@example.com");

        Assert.Equal(expected: "deploy@example.com", actual: pusher.Email);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Pusher first = new(name: "deployer", email: "deploy@example.com");
        Pusher second = new(name: "deployer", email: "deploy@example.com");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        Pusher first = new(name: "deployer", email: "deploy@example.com");
        Pusher second = new(name: "developer", email: "dev@example.com");

        Assert.NotEqual(expected: first, actual: second);
    }
}
