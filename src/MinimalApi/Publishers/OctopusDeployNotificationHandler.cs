using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using MinimalApi.Messages;

namespace MinimalApi.Publishers;

public sealed class OctopusDeployNotificationHandler : INotificationHandler<OctopusDeploy>
{
    private readonly ILogger<OctopusDeployNotificationHandler> _logger;

    public OctopusDeployNotificationHandler(ILogger<OctopusDeployNotificationHandler> logger)
    {
        this._logger = logger;
    }

    public ValueTask Handle(OctopusDeploy notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug($"Octopus: [{notification.Model.Timestamp}]");

        return ValueTask.CompletedTask;
    }
}