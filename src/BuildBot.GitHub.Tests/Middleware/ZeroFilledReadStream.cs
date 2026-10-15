using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.GitHub.Tests.Middleware;

// Produces a body of any length without allocating it, so the over-limit path can be tested without a 25 MB array.
internal sealed class ZeroFilledReadStream : Stream
{
    private long _remaining;

    public ZeroFilledReadStream(long length)
    {
        this._remaining = length;
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
        return this.Fill(buffer.AsSpan(start: offset, length: count));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(this.Fill(buffer.Span));
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

    private int Fill(Span<byte> destination)
    {
        int count = (int)Math.Min(val1: destination.Length, val2: this._remaining);
        destination[..count].Clear();
        this._remaining -= count;

        return count;
    }
}
