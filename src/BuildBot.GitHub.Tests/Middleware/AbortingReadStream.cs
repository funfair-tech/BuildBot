using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub.Tests.Middleware;

// Mimics a client disconnect: the request is aborted and the body read then fails with an IOException.
internal sealed class AbortingReadStream : Stream
{
    private readonly CancellationTokenSource _requestAborted;

    public AbortingReadStream(CancellationTokenSource requestAborted)
    {
        this._requestAborted = requestAborted;
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

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await this._requestAborted.CancelAsync();

        throw new IOException("The client disconnected.");
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
