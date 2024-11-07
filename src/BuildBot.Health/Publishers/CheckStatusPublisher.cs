using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.ServiceModel.ComponentStatus;
using Mediator;

namespace BuildBot.Health.Publishers;

public sealed class CheckStatusPublisher : ICommandHandler<CheckStatus, IReadOnlyList<ServiceStatus>>
{
    private readonly IServerStatus _serverStatus;

    public CheckStatusPublisher(IServerStatus serverStatus)
    {
        this._serverStatus = serverStatus;
    }

    public ValueTask<IReadOnlyList<ServiceStatus>> Handle(CheckStatus command, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(this._serverStatus.CurrentStatus());
    }
}