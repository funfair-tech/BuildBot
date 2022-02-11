using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord;
using Microsoft.Extensions.Hosting;

namespace BuildBot.Services;

public sealed class BotService : IHostedService
{
    private readonly DiscordBot _bot;

    public BotService(DiscordBot bot)
    {
        this._bot = bot ?? throw new ArgumentNullException(nameof(bot));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return this._bot.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return this._bot.StopAsync();
    }
}