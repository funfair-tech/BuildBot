using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationMessageReceivedNotificationHandler : INotificationHandler<CloudFormationMessageReceived>
{
    private readonly ILogger<CloudFormationMessageReceivedNotificationHandler> _logger;
    private readonly SnsNotificationOptions _options;

    public CloudFormationMessageReceivedNotificationHandler(SnsNotificationOptions options, ILogger<CloudFormationMessageReceivedNotificationHandler> logger)
    {
        this._options = options;
        this._logger = logger;
    }

    public ValueTask Handle(CloudFormationMessageReceived notification, CancellationToken cancellationToken)
    {
        if (!this._options.IsValidArn(notification.TopicArn))
        {
            return ValueTask.CompletedTask;
        }

        this._logger.LogWarning(message: "CLOUDFORMATION: Received message from {TopicArn} with {MessageId} at {Timestamp}", notification.TopicArn, notification.MessageId, notification.Timestamp);

        foreach ((string key, string value) in notification.Properties)
        {
            this._logger.LogWarning(message: "CLOUDFORMATION: Property: {Key} = {Value}", key, value);
        }

        // TODO: Handle publish to discord

        return ValueTask.CompletedTask;
    }
}