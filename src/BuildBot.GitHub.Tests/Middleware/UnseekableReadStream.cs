using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub.Tests.Middleware;

// Kestrel request bodies cannot seek, so this stands in for one to show the middleware buffers the body itself.
internal sealed class UnseekableReadStream : Stream
{
    private readonly Stream _inner;

    public UnseekableReadStream(Stream inner)
    {
        this._inner = inner;
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
        return this._inner.Read(buffer: buffer, offset: offset, count: count);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return this._inner.ReadAsync(buffer: buffer, cancellationToken: cancellationToken);
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
}
