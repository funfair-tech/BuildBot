using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using Microsoft.Extensions.Hosting;

namespace BuildBot.Discord.Services;

public sealed class BotService : IHostedService, IDisposable
{
    private static readonly TimeSpan InterMessageDelay = TimeSpan.FromSeconds(1);
    private readonly IDiscordBot _bot;
    private readonly IMessageChannel<BotMessage> _botMessageChannel;
    private readonly IMessageChannel<BotReleaseMessage> _botReleaseMessageChannel;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public BotService(
        IDiscordBot bot,
        IMessageChannel<BotMessage> botMessageChannel,
        IMessageChannel<BotReleaseMessage> botReleaseMessageChannel
    )
    {
        this._bot = bot ?? throw new ArgumentNullException(nameof(bot));
        this._botMessageChannel = botMessageChannel;
        this._botReleaseMessageChannel = botReleaseMessageChannel;

        this._botMessageChannel.ReadAllAsync(this._cancellationTokenSource.Token)
            .ToObservable()
            .Delay(InterMessageDelay)
            .Select(message =>
                Observable.FromAsync(ct => this.PublishMessageAsync(message: message, cancellationToken: ct).AsTask())
            )
            .Concat()
            .Subscribe(this._cancellationTokenSource.Token);

        this._botReleaseMessageChannel.ReadAllAsync(this._cancellationTokenSource.Token)
            .ToObservable()
            .Delay(InterMessageDelay)
            .Select(message =>
                Observable.FromAsync(ct => this.PublishMessageAsync(message: message, cancellationToken: ct).AsTask())
            )
            .Concat()
            .Subscribe(this._cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        this._cancellationTokenSource.Cancel();
        this._cancellationTokenSource.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return this._bot.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await this._cancellationTokenSource.CancelAsync();
        await this._bot.StopAsync(cancellationToken);
    }

    private ValueTask PublishMessageAsync(BotMessage message, in CancellationToken cancellationToken)
    {
        return this._bot.PublishAsync(builder: message.Message, cancellationToken: cancellationToken);
    }

    private ValueTask PublishMessageAsync(BotReleaseMessage message, in CancellationToken cancellationToken)
    {
        return this._bot.PublishToReleaseChannelAsync(builder: message.Message, cancellationToken: cancellationToken);
    }
}
