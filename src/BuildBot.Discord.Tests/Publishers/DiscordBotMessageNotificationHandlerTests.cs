using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Publishers;
using Discord;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Publishers;

public sealed class DiscordBotMessageNotificationHandlerTests : TestBase
{
    [Fact]
    public async Task Handle_LogsAndPublishesToChannel()
    {
        ILogger<DiscordBotMessageNotificationHandler> logger =
            this.GetTypedLogger<DiscordBotMessageNotificationHandler>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        IMessageChannel<BotMessage> channel = GetSubstitute<IMessageChannel<BotMessage>>();

        DiscordBotMessageNotificationHandler handler = new(messageChannel: channel, logger: logger);

        EmbedBuilder builder = new EmbedBuilder().WithTitle("Test Notification");
        BotMessage notification = new(builder);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        logger.Received(1).IsEnabled(LogLevel.Information);
        await channel
            .Received(1)
            .PublishAsync(message: Arg.Any<BotMessage>(), cancellationToken: Arg.Any<CancellationToken>());
    }
}
