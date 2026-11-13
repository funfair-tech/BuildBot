using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Tests;

[Collection(ThreadPoolCollection.NAME)]
public sealed class ProgramTests : TestBase
{
    private static readonly Uri PingUri = new("http://127.0.0.1:8080/ping");

    [Fact]
    public async Task SelfTestSwitchExitsWithoutStartingServerAsync()
    {
        int exitCode = await Program.Main(["--self-test"]);

        Assert.Equal(expected: 0, actual: exitCode);
    }

    [Fact]
    public async Task HealthCheckSwitchFailsWhenNothingIsListeningAsync()
    {
        int exitCode = await Program.Main(["--health-check", $"http://127.0.0.1:{GetUnusedPort()}/ping"]);

        Assert.NotEqual(expected: 0, actual: exitCode);
    }

    [Fact]
    public async Task ServerExitsWithErrorWhenStartupFailsAsync()
    {
        using (TestConfigFolder contentRoot = new())
        {
            string missingFolder = Path.Combine(path1: contentRoot.FullName, path2: "missing");

            int exitCode = await Program.Main(["--contentRoot", missingFolder]);

            Assert.Equal(expected: 1, actual: exitCode);
        }
    }

    [Fact]
    public async Task ServerRunsUntilCancelledAsync()
    {
        using (TestConfigFolder configFolder = new())
        {
            await configFolder.WriteAppSettingsAsync(
                discordWebhookUrl: TestConfigFolder.DISCORD_WEBHOOK_URL,
                cancellationToken: this.CancellationToken()
            );

            using (
                CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken())
            )
            {
                Task<int> server = Program
                    .RunServerAsync(args: [], configPath: configFolder.FullName, cancellationToken: stop.Token)
                    .AsTask();

                await WaitUntilListeningAsync(server: server, cancellationToken: this.CancellationToken());

                await stop.CancelAsync();

                Assert.Equal(expected: 0, actual: await server);
            }
        }
    }

    private static int GetUnusedPort()
    {
        using (TcpListener listener = new(localaddr: IPAddress.Loopback, port: 0))
        {
            listener.Start();

            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }

    private static async ValueTask WaitUntilListeningAsync(Task server, CancellationToken cancellationToken)
    {
        using (HttpClient client = new())
        {
            while (!server.IsCompleted)
            {
                try
                {
                    using (await client.GetAsync(requestUri: PingUri, cancellationToken: cancellationToken))
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                    await Task.Delay(millisecondsDelay: 100, cancellationToken: cancellationToken);
                }
            }
        }
    }
}
