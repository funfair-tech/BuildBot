using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BuildBot.Discord.Services;

public sealed class MessageChannel<T> : IMessageChannel<T>
{
    private readonly Channel<T> _channel;

    public MessageChannel()
    {
        this._channel = Channel.CreateUnbounded<T>();
    }

    public ValueTask PublishAsync(T message, CancellationToken cancellationToken)
    {
        return this._channel.Writer.WriteAsync(item: message, cancellationToken: cancellationToken);
    }

    public ValueTask<T> ReceiveAsync(CancellationToken cancellationToken)
    {
        return this._channel.Reader.ReadAsync(cancellationToken);
    }

    public IAsyncEnumerable<T> ReadAllAsync(in CancellationToken cancellationToken)
    {
        return this._channel.Reader.ReadAllAsync(cancellationToken);
    }
}