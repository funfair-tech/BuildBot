using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using MinimalApi.Messages;

namespace MinimalApi.Publishers;

public sealed class GithubPingNotificationHandler : INotificationHandler<GithubPing>
{
    private readonly ILogger<GithubPingNotificationHandler> _logger;

    public GithubPingNotificationHandler(ILogger<GithubPingNotificationHandler> logger)
    {
        this._logger = logger;
    }

    public ValueTask Handle(GithubPing notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug($"Github: [{notification.Model.HookId}] {notification.Model.Zen}");

        return ValueTask.CompletedTask;
    }
}