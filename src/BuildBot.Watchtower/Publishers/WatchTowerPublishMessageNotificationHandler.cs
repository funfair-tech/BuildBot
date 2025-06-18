using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Watchtower.Models;
using Discord;
using Mediator;

namespace BuildBot.Watchtower.Publishers;

public sealed class WatchTowerPublishMessageNotificationHandler : INotificationHandler<WatchTowerPublishMessage>
{
    private readonly IMediator _mediator;

    public WatchTowerPublishMessageNotificationHandler(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public ValueTask Handle(WatchTowerPublishMessage notification, CancellationToken cancellationToken)
    {
        return this._mediator.Publish(new BotMessage(BuildMessage(notification)), cancellationToken: cancellationToken);
    }

    private static EmbedBuilder BuildMessage(WatchTowerPublishMessage notification)
    {
        return new EmbedBuilder().WithTitle(notification.Model.Title)
                                 .WithDescription(notification.Model.Message);
    }
}