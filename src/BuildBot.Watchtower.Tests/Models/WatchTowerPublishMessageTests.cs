using BuildBot.ServiceModel.Watchtower;
using BuildBot.Watchtower.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Watchtower.Tests.Models;

public sealed class WatchTowerPublishMessageTests : TestBase
{
    [Fact]
    public void ModelPropertyMatchesConstructorArgument()
    {
        WatchTowerMessage model = new(Message: "Service deployed", Title: "Deployment");

        WatchTowerPublishMessage notification = new(model);

        Assert.Equal(expected: model, actual: notification.Model);
    }

    [Fact]
    public void TwoNotificationsWithSameModelAreEqual()
    {
        WatchTowerMessage model = new(Message: "Service deployed", Title: "Deployment");

        WatchTowerPublishMessage first = new(model);
        WatchTowerPublishMessage second = new(model);

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoNotificationsWithDifferentModelsAreNotEqual()
    {
        WatchTowerPublishMessage first = new(new WatchTowerMessage(Message: "Service deployed", Title: "Deployment"));
        WatchTowerPublishMessage second = new(new WatchTowerMessage(Message: "Service failed", Title: "Failure"));

        Assert.NotEqual(expected: first, actual: second);
    }
}
