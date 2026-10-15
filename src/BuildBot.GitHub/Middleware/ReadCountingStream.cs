using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub.Middleware;

[DebuggerDisplay("{BytesRead} bytes read")]
public sealed class ReadCountingStream : Stream
{
    private readonly Stream _inner;

    public ReadCountingStream(Stream inner)
    {
        this._inner = inner;
    }

    public long BytesRead { get; private set; }

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
        return this.Count(this._inner.Read(buffer: buffer, offset: offset, count: count));
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return this.Count(await this._inner.ReadAsync(buffer: buffer, cancellationToken: cancellationToken));
    }

    // The base Stream routes these through BeginRead and the synchronous Read, which Kestrel request bodies reject.
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return this.ReadAsync(
                buffer: buffer.AsMemory(start: offset, length: count),
                cancellationToken: cancellationToken
            )
            .AsTask();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToAsyncResult.Begin(
            this.ReadAsync(buffer: buffer, offset: offset, count: count, cancellationToken: CancellationToken.None),
            callback: callback,
            state: state
        );
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return TaskToAsyncResult.End<int>(asyncResult);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._inner.Dispose();
        }

        base.Dispose(disposing);
    }

    private int Count(int read)
    {
        this.BytesRead += read;

        return read;
    }
}
