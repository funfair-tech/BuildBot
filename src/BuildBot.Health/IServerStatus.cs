using System.Collections.Generic;
using BuildBot.ServiceModel.ComponentStatus;

namespace BuildBot.Health;

public interface IServerStatus
{
    IReadOnlyList<ServiceStatus> CurrentStatus();
}
