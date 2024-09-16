using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BuildBot.Helpers;
using Microsoft.AspNetCore.Builder;

namespace BuildBot;

public static class Program
{
    private const int MIN_THREADS = 32;

    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    public static async Task<int> Main(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        try
        {
            await using (WebApplication app = ServerStartup.CreateApp(args))
            {
                Console.WriteLine("App Created");
                await app.ConfigureEndpoints()
                         .RunAsync();

                return 0;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred:");
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);

            return 1;
        }
    }
}