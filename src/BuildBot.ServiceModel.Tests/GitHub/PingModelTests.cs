using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class PingModelTests : TestBase
{
    [Fact]
    public void ZenPropertyMatchesConstructorArgument()
    {
        PingModel model = new(zen: "Design for failure.", hookId: "12345");

        Assert.Equal(expected: "Design for failure.", actual: model.Zen);
    }

    [Fact]
    public void HookIdPropertyMatchesConstructorArgument()
    {
        PingModel model = new(zen: "Design for failure.", hookId: "12345");

        Assert.Equal(expected: "12345", actual: model.HookId);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        PingModel first = new(zen: "Design for failure.", hookId: "12345");
        PingModel second = new(zen: "Design for failure.", hookId: "12345");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        PingModel first = new(zen: "Design for failure.", hookId: "12345");
        PingModel second = new(zen: "Keep it logically awesome.", hookId: "67890");

        Assert.NotEqual(expected: first, actual: second);
    }
}
