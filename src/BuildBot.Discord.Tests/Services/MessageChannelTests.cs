using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord;
using BuildBot.Discord.Services;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class MessageChannelTests : TestBase
{
    [Fact]
    public async Task PublishAsync_ThenReadAllAsync_ReturnsPublishedMessage()
    {
        MessageChannel<string> channel = new();
        const string testMessage = "hello";

        await channel.PublishAsync(message: testMessage, cancellationToken: this.CancellationToken());

        using CancellationTokenSource cts = new();
        cts.CancelAfter(5000);

        await using IAsyncEnumerator<string> enumerator = channel.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        bool hasValue = await enumerator.MoveNextAsync();

        Assert.True(condition: hasValue, userMessage: "Expected to receive a published message");

        string received = enumerator.Current;

        Assert.Equal(expected: testMessage, actual: received);
    }
}
