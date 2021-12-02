using System.IO;
using System.Threading.Tasks;
using BuildBot.Discord;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using (IHost host = CreateHostBuilder(args))
        {
            DiscordBot bot = host.Services.GetRequiredService<DiscordBot>();

            // waiting a Task is normally a big no no because of deadlocks, but we're in a start up task here so it should be ok
            await bot.StartAsync();

            await host.RunAsync();

            await bot.StopAsync();
        }
    }

    private static IHost CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(webHostBuilder =>
                                             {
                                                 webHostBuilder.UseKestrel()
                                                               .UseContentRoot(Directory.GetCurrentDirectory())
                                                               .UseStartup<Startup>()
                                                               .UseUrls("http://*:49781");
                                             })
                   .UseWindowsService()
                   .UseSystemd()
                   .Build();
    }
}