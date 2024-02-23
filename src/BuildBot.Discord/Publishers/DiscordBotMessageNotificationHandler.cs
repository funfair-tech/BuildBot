using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using Mediator;

namespace BuildBot.Discord.Publishers;

public sealed class DiscordBotMessageNotificationHandler : INotificationHandler<BotMessage>
{
    private readonly IDiscordBot _discordBot;

    public DiscordBotMessageNotificationHandler(IDiscordBot discordBot)
    {
        this._discordBot = discordBot;
    }

    public ValueTask Handle(BotMessage notification, CancellationToken cancellationToken)
    {
        return this._discordBot.PublishAsync(builder: notification.Message, cancellationToken: cancellationToken);
    }
}