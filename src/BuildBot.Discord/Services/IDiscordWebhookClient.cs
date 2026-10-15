using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

public interface IDiscordWebhookClient
{
    ValueTask<WebhookSendResult> SendAsync(Uri webhookUrl, WebhookPayload payload, CancellationToken cancellationToken);
}
