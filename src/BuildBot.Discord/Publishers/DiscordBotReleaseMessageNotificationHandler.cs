using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using Mediator;

namespace BuildBot.Discord.Publishers;

public sealed class DiscordBotReleaseMessageNotificationHandler : INotificationHandler<BotReleaseMessage>
{
    private readonly IMessageChannel<BotReleaseMessage> _messageChannel;

    public DiscordBotReleaseMessageNotificationHandler(IMessageChannel<BotReleaseMessage> messageChannel)
    {
        this._messageChannel = messageChannel;
    }

    public ValueTask Handle(BotReleaseMessage notification, CancellationToken cancellationToken)
    {
        return this._messageChannel.PublishAsync(message: notification, cancellationToken: cancellationToken);
    }
}
