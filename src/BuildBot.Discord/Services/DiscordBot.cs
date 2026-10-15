using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.Discord.Services.LoggingExtensions;
using BuildBot.Discord.Services.Models;
using BuildBot.ServiceModel.ComponentStatus;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services;

public sealed class DiscordBot : IDiscordBot, IComponentStatus
{
    private readonly DiscordBotConfiguration _botConfiguration;
    private readonly IDiscordWebhookClient _client;
    private readonly ILogger<DiscordBot> _logger;

    public DiscordBot(
        IDiscordWebhookClient client,
        DiscordBotConfiguration botConfiguration,
        ILogger<DiscordBot> logger
    )
    {
        this._client = client;
        this._botConfiguration = botConfiguration;
        this._logger = logger;
    }

    public ServiceStatus GetStatus()
    {
        return new(Name: "Discord", Ok: this._botConfiguration.IsConfigured);
    }

    public async ValueTask PublishAsync(DiscordEmbed embed, CancellationToken cancellationToken)
    {
        if (!this._botConfiguration.IsConfigured)
        {
            this._logger.LogWebhookNotConfigured(embed.Title);

            return;
        }

        // Nothing may escape: BotService sends from an Rx subscription which an exception would terminate.
        try
        {
            this._logger.LogSendingMessage(embed.Title);

            WebhookSendResult result = await this._client.SendAsync(
                webhookUrl: this._botConfiguration.WebhookUrl,
                payload: EmbedMapper.ToPayload(embed),
                cancellationToken: cancellationToken
            );

            this.LogResult(embed: embed, result: result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this._logger.LogSendCancelled(embed.Title);
        }
        catch (Exception exception)
        {
            this._logger.LogSendFailed(title: embed.Title, exception: exception);
        }
    }

    private void LogResult(DiscordEmbed embed, in WebhookSendResult result)
    {
        if (result.Success)
        {
            this._logger.LogSentMessage(embed.Title);
        }
        else
        {
            this._logger.LogSendRejected(title: embed.Title, statusCode: result.StatusCode);
        }
    }
}
