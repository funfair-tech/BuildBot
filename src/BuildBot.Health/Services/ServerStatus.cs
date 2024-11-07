using System;
using System.Collections.Generic;
using System.Linq;
using BuildBot.ServiceModel.ComponentStatus;

namespace BuildBot.Health.Services;

public sealed class ServerStatus : IServerStatus
{
    private readonly IReadOnlyList<IComponentStatus> _componentStatusChecks;

    public ServerStatus(IEnumerable<IComponentStatus> componentStatusChecks)
    {
        this._componentStatusChecks = [..componentStatusChecks];
    }

    public IReadOnlyList<ServiceStatus> CurrentStatus()
    {
        return
        [
            .. this._componentStatusChecks.Select(x => x.GetStatus())
                   .OrderBy(keySelector: s => s.Name, comparer: StringComparer.Ordinal)
        ];
    }
}