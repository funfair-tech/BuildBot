using BuildBot.ServiceModel.Watchtower;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.Watchtower;

public sealed class WatchTowerMessageTests : TestBase
{
    [Fact]
    public void MessagePropertyMatchesConstructorArgument()
    {
        WatchTowerMessage message = new(Message: "Service deployed successfully", Title: "Deployment");

        Assert.Equal(expected: "Service deployed successfully", actual: message.Message);
    }

    [Fact]
    public void TitlePropertyMatchesConstructorArgument()
    {
        WatchTowerMessage message = new(Message: "Service deployed successfully", Title: "Deployment");

        Assert.Equal(expected: "Deployment", actual: message.Title);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        WatchTowerMessage first = new(Message: "Service deployed successfully", Title: "Deployment");
        WatchTowerMessage second = new(Message: "Service deployed successfully", Title: "Deployment");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentMessagesAreNotEqual()
    {
        WatchTowerMessage first = new(Message: "Service deployed successfully", Title: "Deployment");
        WatchTowerMessage second = new(Message: "Service failed to deploy", Title: "Failure");

        Assert.NotEqual(expected: first, actual: second);
    }
}
