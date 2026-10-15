using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.ServiceModel.Watchtower;
using BuildBot.Watchtower.Models;
using BuildBot.Watchtower.Publishers;
using FunFair.Test.Common;
using Mediator;
using NSubstitute;
using Xunit;

namespace BuildBot.Watchtower.Tests.Publishers;

public sealed class WatchTowerPublishMessageNotificationHandlerTests : TestBase
{
    [Fact]
    public async Task HandlePublishesBotMessageViaMediatorAsync()
    {
        IMediator mediator = GetSubstitute<IMediator>();
        WatchTowerPublishMessageNotificationHandler handler = new(mediator);

        WatchTowerMessage model = new(Message: "Service deployed", Title: "Deployment");
        WatchTowerPublishMessage notification = new(model);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishedEmbedHasExpectedContentAsync()
    {
        IMediator mediator = GetSubstitute<IMediator>();
        WatchTowerPublishMessageNotificationHandler handler = new(mediator);

        await handler.Handle(
            notification: new(new WatchTowerMessage(Message: "Service deployed", Title: "Deployment")),
            cancellationToken: this.CancellationToken()
        );

        DiscordEmbed embed = mediator
            .ReceivedCalls()
            .Select(static call => call.GetArguments()[0])
            .OfType<BotMessage>()
            .Single()
            .Message;
        Assert.Equal(expected: "Deployment", actual: embed.Title);
        Assert.Equal(expected: "Service deployed", actual: embed.Description);
        Assert.Null(embed.Url);
        Assert.Null(embed.Color);
        Assert.Empty(embed.Fields);
    }
}
