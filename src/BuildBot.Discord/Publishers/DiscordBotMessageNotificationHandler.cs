using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Publishers.LoggingExtensions;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Publishers;

public sealed class DiscordBotMessageNotificationHandler : INotificationHandler<BotMessage>
{
    private readonly IMessageChannel<BotMessage> _messageChannel;
    private readonly ILogger<DiscordBotMessageNotificationHandler> _logger;

    public DiscordBotMessageNotificationHandler(
        IMessageChannel<BotMessage> messageChannel,
        ILogger<DiscordBotMessageNotificationHandler> logger
    )
    {
        this._messageChannel = messageChannel;
        this._logger = logger;
    }

    public ValueTask Handle(BotMessage notification, CancellationToken cancellationToken)
    {
        this._logger.QueueingDiscordMessage(notification.Message.Title);

        return this._messageChannel.PublishAsync(
            message: notification,
            cancellationToken: cancellationToken
        );
    }
}
