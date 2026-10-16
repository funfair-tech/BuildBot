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
        return new(Name: "Discord", Ok: true);
    }

    public async ValueTask PublishAsync(DiscordEmbed embed, CancellationToken cancellationToken)
    {
        this._logger.LogSendingMessage(embed.Title);

        WebhookSendResult result = await this._client.SendAsync(
            webhookUrl: this._botConfiguration.WebhookUrl,
            payload: EmbedMapper.ToPayload(embed),
            cancellationToken: cancellationToken
        );

        this.LogResult(embed: embed, result: result);
    }

    private void LogResult(DiscordEmbed embed, in WebhookSendResult result)
    {
        if (result.Success)
        {
            this._logger.LogSentMessage(embed.Title);
        }
        else
        {
            this._logger.LogSendRejected(
                title: embed.Title,
                statusCode: result.StatusCode,
                responseBody: result.ResponseBody
            );
        }
    }
}
