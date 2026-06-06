using System.Collections.Generic;
using System.Threading.Tasks;
using BuildBot.Health;
using BuildBot.Health.Publishers;
using BuildBot.ServiceModel.ComponentStatus;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace BuildBot.Health.Tests.Publishers;

public sealed class CheckStatusPublisherTests : TestBase
{
    [Fact]
    public async Task Handle_ReturnsStatusFromServerStatusAsync()
    {
        IServerStatus serverStatus = GetSubstitute<IServerStatus>();
        IReadOnlyList<ServiceStatus> expected = [new ServiceStatus(Name: "db", Ok: true)];
        serverStatus.CurrentStatus().Returns(expected);

        CheckStatusPublisher publisher = new(serverStatus);
        CheckStatus command = new(Source: "test");

        IReadOnlyList<ServiceStatus> result = await publisher.Handle(
            command: command,
            cancellationToken: this.CancellationToken()
        );

        Assert.Same(expected: expected, actual: result);
    }

    [Fact]
    public async Task Handle_WithNullSource_ReturnsStatusFromServerStatusAsync()
    {
        IServerStatus serverStatus = GetSubstitute<IServerStatus>();
        IReadOnlyList<ServiceStatus> expected = [];
        serverStatus.CurrentStatus().Returns(expected);

        CheckStatusPublisher publisher = new(serverStatus);
        CheckStatus command = new(Source: null);

        IReadOnlyList<ServiceStatus> result = await publisher.Handle(
            command: command,
            cancellationToken: this.CancellationToken()
        );

        Assert.Same(expected: expected, actual: result);
    }
}
