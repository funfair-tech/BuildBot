using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Middleware;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Middleware;

public sealed class ReadCountingStreamTests : TestBase
{
    private static readonly byte[] Content = [1, 2, 3, 4, 5];

    [Fact]
    public void SynchronousReadsAreCounted()
    {
        using MemoryStream inner = new(Content);
        using ReadCountingStream stream = new(inner);
        byte[] buffer = new byte[3];

        int first = stream.Read(buffer: buffer, offset: 0, count: buffer.Length);
        int second = stream.Read(buffer: buffer, offset: 0, count: buffer.Length);

        Assert.Equal(expected: 3, actual: first);
        Assert.Equal(expected: 2, actual: second);
        Assert.Equal(expected: Content.Length, actual: stream.BytesRead);
    }

    [Fact]
    public async Task AsynchronousReadsAreCountedAsync()
    {
        await using MemoryStream inner = new(Content);
        await using ReadCountingStream stream = new(inner);
        await using MemoryStream copy = new();

        await stream.CopyToAsync(destination: copy, cancellationToken: this.CancellationToken());

        Assert.Equal(expected: Content, actual: copy.ToArray());
        Assert.Equal(expected: Content.Length, actual: stream.BytesRead);
    }

    [Fact]
    public async Task ArrayAsynchronousReadsUseTheInnerAsynchronousReadAndAreCountedAsync()
    {
        await using ReadCountingStream stream = new(new AsyncOnlyReadStream(new MemoryStream(Content)));
        byte[] buffer = new byte[Content.Length + 2];

        // Bound to a delegate because this test targets the array overload, which CA1835 rejects at a direct call.
        Func<byte[], int, int, CancellationToken, Task<int>> readArrayAsync = stream.ReadAsync;

        int first = await readArrayAsync(arg1: buffer, arg2: 1, arg3: 3, arg4: this.CancellationToken());
        int second = await readArrayAsync(arg1: buffer, arg2: 4, arg3: 3, arg4: this.CancellationToken());

        Assert.Equal(expected: 3, actual: first);
        Assert.Equal(expected: 2, actual: second);
        Assert.Equal(expected: Content, actual: buffer[1..^1]);
        Assert.Equal(expected: Content.Length, actual: stream.BytesRead);
    }

    [Fact]
    public async Task BeginReadUsesTheInnerAsynchronousReadAndIsCountedAsync()
    {
        await using ReadCountingStream stream = new(new AsyncOnlyReadStream(new MemoryStream(Content)));
        byte[] buffer = new byte[Content.Length];

        int read = await Task<int>.Factory.FromAsync(
            beginMethod: stream.BeginRead,
            endMethod: stream.EndRead,
            arg1: buffer,
            arg2: 0,
            arg3: buffer.Length,
            state: null
        );

        Assert.Equal(expected: Content.Length, actual: read);
        Assert.Equal(expected: Content, actual: buffer);
        Assert.Equal(expected: Content.Length, actual: stream.BytesRead);
    }

    [Fact]
    public async Task IsReadOnlyAndForwardOnlyAsync()
    {
        await using MemoryStream inner = new(Content);
        await using ReadCountingStream stream = new(inner);

        await stream.FlushAsync(this.CancellationToken());

        Assert.True(condition: stream.CanRead, userMessage: "Should be readable");
        Assert.False(condition: stream.CanSeek, userMessage: "Should not be seekable");
        Assert.False(condition: stream.CanWrite, userMessage: "Should not be writable");
        AssertSeekingAndWritingAreUnsupported(stream);
    }

    private static void AssertSeekingAndWritingAreUnsupported(Stream stream)
    {
        Assert.Throws<NotSupportedException>(() => stream.Length);
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        Assert.Throws<NotSupportedException>(() => stream.Seek(offset: 0, origin: SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(1));
    }
}
