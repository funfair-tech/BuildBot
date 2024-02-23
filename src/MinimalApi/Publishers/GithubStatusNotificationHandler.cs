using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using MinimalApi.Messages;

namespace MinimalApi.Publishers;

public sealed class GithubStatusNotificationHandler : INotificationHandler<GithubStatus>
{
    private readonly ILogger<GithubStatusNotificationHandler> _logger;

    public GithubStatusNotificationHandler(ILogger<GithubStatusNotificationHandler> logger)
    {
        this._logger = logger;
    }

    public ValueTask Handle(GithubStatus notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug($"Github: [{notification.Model.Context}]");

        return ValueTask.CompletedTask;
    }
}