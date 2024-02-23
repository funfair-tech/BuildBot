using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.Discord;

public interface IMessageChannel<T>
{
    ValueTask PublishAsync(T message, CancellationToken cancellationToken);

    IAsyncEnumerable<T> ReadAllAsync(in CancellationToken cancellationToken);
}