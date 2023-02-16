using System.Threading;
using System.Threading.Tasks;
using BuildBot.Helpers;
using Microsoft.Extensions.Hosting;

namespace BuildBot;

public static class Program
{
    private const int MIN_THREADS = 32;

    public static async Task Main(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        using (IHost host = ServerStartup.CreateWebHost<Startup>(args: args, httpPort: 49781, httpsPort: 0, h2Port: 0, configurationFiledPath: ApplicationConfigLocator.ConfigurationFilesPath))
        {
            await host.RunAsync(CancellationToken.None);
        }
    }
}