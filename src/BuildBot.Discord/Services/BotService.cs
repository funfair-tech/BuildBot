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
    private readonly DiscordBot _bot;
    private readonly IMessageChannel<BotMessage> _botMessageChannel;
    private readonly IMessageChannel<BotReleaseMessage> _botReleaseMessageChannel;
    private readonly IDisposable _messageSubscription;
    private readonly IDisposable _releaseMessageSubscription;

    public BotService(DiscordBot bot, IMessageChannel<BotMessage> botMessageChannel, IMessageChannel<BotReleaseMessage> botReleaseMessageChannel)
    {
        this._bot = bot ?? throw new ArgumentNullException(nameof(bot));
        this._botMessageChannel = botMessageChannel;
        this._botReleaseMessageChannel = botReleaseMessageChannel;

        this._messageSubscription = this
            ._botMessageChannel.ReadAllAsync(CancellationToken.None)
            .ToObservable()
            .Delay(InterMessageDelay)
            .Select(message => Observable.FromAsync(ct => this.PublishMessageAsync(message: message, cancellationToken: ct).AsTask()))
            .Concat()
            .Subscribe();

        this._releaseMessageSubscription = this
            ._botReleaseMessageChannel.ReadAllAsync(CancellationToken.None)
            .ToObservable()
            .Delay(InterMessageDelay)
            .Select(message => Observable.FromAsync(ct => this.PublishMessageAsync(message: message, cancellationToken: ct).AsTask()))
            .Concat()
            .Subscribe();
    }

    public void Dispose()
    {
        this._messageSubscription.Dispose();
        this._releaseMessageSubscription.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return this._bot.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return this._bot.StopAsync();
    }

    private ValueTask PublishMessageAsync(BotMessage message, CancellationToken cancellationToken)
    {
        return this._bot.PublishAsync(builder: message.Message, cancellationToken: cancellationToken);
    }

    private ValueTask PublishMessageAsync(BotReleaseMessage message, CancellationToken cancellationToken)
    {
        return this._bot.PublishToReleaseChannelAsync(builder: message.Message, cancellationToken: cancellationToken);
    }
}
