using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services.LoggingExtensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services;

public sealed class DiscordMessageSender : BackgroundService
{
    private readonly IDiscordBot _bot;
    private readonly IMessageChannel<BotMessage> _botMessageChannel;
    private readonly ILogger<DiscordMessageSender> _logger;

    public DiscordMessageSender(
        IDiscordBot bot,
        IMessageChannel<BotMessage> botMessageChannel,
        ILogger<DiscordMessageSender> logger
    )
    {
        this._bot = bot;
        this._botMessageChannel = botMessageChannel;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BotMessage message in this._botMessageChannel.ReadAllAsync(stoppingToken))
        {
            await this.PublishMessageAsync(message: message, stoppingToken: stoppingToken);
        }
    }

    private async ValueTask PublishMessageAsync(BotMessage message, CancellationToken stoppingToken)
    {
        // One failed message must not end the loop; only shutting down may.
        try
        {
            await this._bot.PublishAsync(embed: message.Message, cancellationToken: stoppingToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            this._logger.LogPublishFailed(title: message.Message.Title, exception: exception);
        }
    }
}
