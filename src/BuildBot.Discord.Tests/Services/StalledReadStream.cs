using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.Discord.Tests.Services;

internal sealed class StalledReadStream : Stream
{
    private readonly SemaphoreSlim _readStarted = new(initialCount: 0);

    public Task WaitForReadStartedAsync(CancellationToken cancellationToken)
    {
        return this._readStarted.WaitAsync(cancellationToken);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._readStarted.Dispose();
        }

        base.Dispose(disposing);
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        this._readStarted.Release();

        // Only cancellation ends the read, as with a server that sent headers and then stopped sending.
        await Task.Delay(millisecondsDelay: Timeout.Infinite, cancellationToken: cancellationToken);

        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
