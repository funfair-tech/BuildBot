using System.Collections.Generic;
using BuildBot.Health.Services;
using BuildBot.ServiceModel.ComponentStatus;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace BuildBot.Health.Tests.Services;

public sealed class ServerStatusTests : TestBase
{
    [Fact]
    public void CurrentStatus_WithNoComponents_ReturnsEmptyList()
    {
        ServerStatus serverStatus = new(componentStatusChecks: []);

        IReadOnlyList<ServiceStatus> result = serverStatus.CurrentStatus();

        Assert.Empty(result);
    }

    [Fact]
    public void CurrentStatus_WithOneComponent_ReturnsSingleStatus()
    {
        IComponentStatus component = GetSubstitute<IComponentStatus>();
        component.GetStatus().Returns(new ServiceStatus(Name: "db", Ok: true));

        ServerStatus serverStatus = new(componentStatusChecks: [component]);

        IReadOnlyList<ServiceStatus> result = serverStatus.CurrentStatus();

        Assert.Single(result);
        Assert.Equal(expected: new ServiceStatus(Name: "db", Ok: true), actual: result[0]);
    }

    [Fact]
    public void CurrentStatus_WithMultipleComponents_ReturnsSortedByName()
    {
        IComponentStatus componentZ = GetSubstitute<IComponentStatus>();
        componentZ.GetStatus().Returns(new ServiceStatus(Name: "zebra", Ok: true));

        IComponentStatus componentA = GetSubstitute<IComponentStatus>();
        componentA.GetStatus().Returns(new ServiceStatus(Name: "alpha", Ok: false));

        IComponentStatus componentM = GetSubstitute<IComponentStatus>();
        componentM.GetStatus().Returns(new ServiceStatus(Name: "mongo", Ok: true));

        ServerStatus serverStatus = new(componentStatusChecks: [componentZ, componentA, componentM]);

        IReadOnlyList<ServiceStatus> result = serverStatus.CurrentStatus();

        Assert.Equal(expected: 3, actual: result.Count);
        Assert.Equal(expected: "alpha", actual: result[0].Name);
        Assert.Equal(expected: "mongo", actual: result[1].Name);
        Assert.Equal(expected: "zebra", actual: result[2].Name);
    }
}
