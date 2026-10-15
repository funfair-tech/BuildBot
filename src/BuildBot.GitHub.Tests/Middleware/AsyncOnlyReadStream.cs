using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub.Tests.Middleware;

// Kestrel request bodies reject synchronous IO, so this fails any synchronous read the way they do.
internal sealed class AsyncOnlyReadStream : Stream
{
    private readonly Stream _inner;

    public AsyncOnlyReadStream(Stream inner)
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
        throw new InvalidOperationException("Synchronous operations are disallowed.");
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
