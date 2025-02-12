using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using Mediator;

namespace BuildBot.Discord.Publishers;

public sealed class DiscordBotMessageNotificationHandler : INotificationHandler<BotMessage>
{
    private readonly IMessageChannel<BotMessage> _messageChannel;

    public DiscordBotMessageNotificationHandler(IMessageChannel<BotMessage> messageChannel)
    {
        this._messageChannel = messageChannel;
    }

    public ValueTask Handle(BotMessage notification, CancellationToken cancellationToken)
    {
        return this._messageChannel.PublishAsync(message: notification, cancellationToken: cancellationToken);
    }
}
