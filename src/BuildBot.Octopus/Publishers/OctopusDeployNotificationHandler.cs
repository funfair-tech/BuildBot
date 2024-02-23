using System.Threading;
using System.Threading.Tasks;
using BuildBot.Octopus.Models;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.Octopus.Publishers;

public sealed class OctopusDeployNotificationHandler : INotificationHandler<OctopusDeploy>
{
    private readonly ILogger<OctopusDeployNotificationHandler> _logger;

    public OctopusDeployNotificationHandler(ILogger<OctopusDeployNotificationHandler> logger)
    {
        this._logger = logger;
    }

    public ValueTask Handle(OctopusDeploy notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug($"Octopus: [{notification.Model.EventType}]");

        return ValueTask.CompletedTask;
    }
}