using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Helpers;
using BuildBot.Json;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuildBot.Tests;

[Collection(ThreadPoolCollection.NAME)]
public sealed class ServerStartupTests : TestBase
{
    [Theory]
    [InlineData(0, 0, 1, 1, 1)]
    [InlineData(0, 2, 1, 1, 2)]
    [InlineData(2, 0, 1, 2, 1)]
    [InlineData(2, 2, 1, 2, 2)]
    public static void RaisesOnlyTheThreadPoolMinimumsBelowTheTarget(
        int workerOffset,
        int completionPortOffset,
        int targetOffset,
        int expectedWorkerOffset,
        int expectedCompletionPortOffset
    )
    {
        ThreadPool.GetMinThreads(out int originalWorker, out int originalCompletionPort);

        // Offsets keep every value above the server's own minimum so a server started by another test cannot change them.
        int baseline = Math.Max(val1: Math.Max(val1: originalWorker, val2: originalCompletionPort), val2: 32);

        try
        {
            Assert.True(
                ThreadPool.SetMinThreads(
                    workerThreads: baseline + workerOffset,
                    completionPortThreads: baseline + completionPortOffset
                ),
                userMessage: "Could not set the thread pool minimums"
            );

            ServerStartup.SetThreads(baseline + targetOffset);

            ThreadPool.GetMinThreads(out int worker, out int completionPort);
            Assert.Equal(expected: baseline + expectedWorkerOffset, actual: worker);
            Assert.Equal(expected: baseline + expectedCompletionPortOffset, actual: completionPort);
        }
        finally
        {
            ThreadPool.SetMinThreads(workerThreads: originalWorker, completionPortThreads: originalCompletionPort);
        }
    }

    [Fact]
    public async Task UsesTheApplicationSerializationContextForHttpJsonAsync()
    {
        using (TestConfigFolder configFolder = new())
        {
            await configFolder.WriteAppSettingsAsync(
                discordWebhookUrl: TestConfigFolder.DISCORD_WEBHOOK_URL,
                cancellationToken: this.CancellationToken()
            );

            await using (WebApplication app = ServerStartup.CreateApp(args: [], configPath: configFolder.FullName))
            {
                JsonOptions options = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value;

                Assert.Same(
                    expected: AppSerializationContext.Default,
                    actual: options.SerializerOptions.TypeInfoResolverChain[0]
                );
            }
        }
    }
}
