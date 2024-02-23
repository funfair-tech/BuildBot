using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using MinimalApi.Messages;

namespace MinimalApi.Publishers;

public sealed class GithubPushNotificationHandler : INotificationHandler<GithubPush>
{
    private readonly ILogger<GithubPushNotificationHandler> _logger;

    public GithubPushNotificationHandler(ILogger<GithubPushNotificationHandler> logger)
    {
        this._logger = logger;
    }

    public ValueTask Handle(GithubPush notification, CancellationToken cancellationToken)
    {
        this._logger.LogDebug($"Github: [{notification.Model.Ref}]");

        return ValueTask.CompletedTask;
    }
}