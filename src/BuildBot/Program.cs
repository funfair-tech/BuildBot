using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace BuildBot
{
    public static class Program
    {
        public static void Main()
        {
            IWebHost host = new WebHostBuilder().UseKestrel()
                                                .UseContentRoot(Directory.GetCurrentDirectory())
                                                .UseStartup<Startup>()
                                                .UseUrls("http://*:49781")
                                                .Build();

            host.Run();
        }
    }
}