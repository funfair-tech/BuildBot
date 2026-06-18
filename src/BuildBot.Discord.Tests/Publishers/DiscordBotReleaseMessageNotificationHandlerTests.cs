using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Publishers;
using Discord;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Publishers;

public sealed class DiscordBotReleaseMessageNotificationHandlerTests : TestBase
{
    [Fact]
    public async Task Handle_PublishesToChannel()
    {
        IMessageChannel<BotReleaseMessage> channel = GetSubstitute<IMessageChannel<BotReleaseMessage>>();

        DiscordBotReleaseMessageNotificationHandler handler = new(channel);

        EmbedBuilder builder = new EmbedBuilder().WithTitle("Release Notification");
        BotReleaseMessage notification = new(builder);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await channel
            .Received(1)
            .PublishAsync(message: Arg.Any<BotReleaseMessage>(), cancellationToken: Arg.Any<CancellationToken>());
    }
}
