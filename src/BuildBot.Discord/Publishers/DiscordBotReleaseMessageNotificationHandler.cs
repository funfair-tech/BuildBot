using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using Mediator;

namespace BuildBot.Discord.Publishers;

public sealed class DiscordBotReleaseMessageNotificationHandler : INotificationHandler<BotReleaseMessage>
{
    private readonly IDiscordBot _discordBot;

    public DiscordBotReleaseMessageNotificationHandler(IDiscordBot discordBot)
    {
        this._discordBot = discordBot;
    }

    public ValueTask Handle(BotReleaseMessage notification, CancellationToken cancellationToken)
    {
        return this._discordBot.PublishAsync(builder: notification.Message, cancellationToken: cancellationToken);
    }
}