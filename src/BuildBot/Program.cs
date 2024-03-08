using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BuildBot.Helpers;
using Microsoft.AspNetCore.Builder;

namespace BuildBot;

public static class Program
{
    private const int MIN_THREADS = 32;

    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    public static async Task Main(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        await using (WebApplication app = ServerStartup.CreateApp(args))
        {
            await app.ConfigureEndpoints()
                     .RunAsync();
        }
    }
}