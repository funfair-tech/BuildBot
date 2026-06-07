using BuildBot.ServiceModel.ComponentStatus;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.ComponentStatus;

public sealed class ServiceStatusTests : TestBase
{
    [Fact]
    public void NamePropertyMatchesConstructorArgument()
    {
        ServiceStatus status = new(Name: "database", Ok: true);

        Assert.Equal(expected: "database", actual: status.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OkPropertyMatchesConstructorArgument(bool ok)
    {
        ServiceStatus status = new(Name: "database", Ok: ok);

        Assert.Equal(expected: ok, actual: status.Ok);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        ServiceStatus first = new(Name: "database", Ok: true);
        ServiceStatus second = new(Name: "database", Ok: true);

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentNamesAreNotEqual()
    {
        ServiceStatus first = new(Name: "database", Ok: true);
        ServiceStatus second = new(Name: "cache", Ok: true);

        Assert.NotEqual(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentOkValuesAreNotEqual()
    {
        ServiceStatus first = new(Name: "database", Ok: true);
        ServiceStatus second = new(Name: "database", Ok: false);

        Assert.NotEqual(expected: first, actual: second);
    }
}
