using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBot.Discord.Tests.Services;

internal sealed class CountingReadStream : Stream
{
    private readonly long _length;

    public CountingReadStream(long length)
    {
        this._length = length;
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
        return this.Fill(buffer.AsSpan(start: offset, length: count));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(this.Fill(buffer.Span));
    }

    private int Fill(Span<byte> buffer)
    {
        int count = (int)Math.Min(val1: buffer.Length, val2: this._length - this.BytesRead);
        buffer[..count].Fill((byte)'a');
        this.BytesRead += count;

        return count;
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
