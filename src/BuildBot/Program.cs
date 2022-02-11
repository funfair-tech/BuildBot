using System.Threading.Tasks;
using BuildBot.Discord;
using BuildBot.Helpers;
using Microsoft.Extensions.DependencyInjection;
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
            DiscordBot bot = host.Services.GetRequiredService<DiscordBot>();

            // waiting a Task is normally a big no no because of deadlocks, but we're in a start up task here so it should be ok
            await bot.StartAsync();

            await host.RunAsync();

            await bot.StopAsync();
        }
    }
}