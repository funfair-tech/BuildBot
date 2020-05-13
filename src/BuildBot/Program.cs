using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BuildBot
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using (var host = CreateHostBuilder(args))
            {
                await host.RunAsync();
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
}