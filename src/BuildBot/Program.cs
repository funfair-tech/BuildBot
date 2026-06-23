using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Helpers;
using Credfeto.Docker.HealthCheck.Http.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace BuildBot;

public static class Program
{
    private const int MIN_THREADS = 32;

    [SuppressMessage(
        category: "Meziantou.Analyzer",
        checkId: "MA0109: Add an overload with a Span or Memory parameter",
        Justification = "Won't work here"
    )]
    public static async Task<int> Main(string[] args)
    {
        if (HealthCheckClient.IsHealthCheck(args: args, out string? checkUrl))
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(static builder => builder.AddConsole());

            return await HealthCheckClient.ExecuteAsync(
                targetUrl: checkUrl,
                logger: loggerFactory.CreateLogger(nameof(Program)),
                cancellationToken: CancellationToken.None
            );
        }

        return await RunServerAsync(args);
    }

    private static async ValueTask<int> RunServerAsync(string[] args)
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        try
        {
            await using (WebApplication app = ServerStartup.CreateApp(args))
            {
                await RunAsync(app);

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

    private static Task RunAsync(WebApplication application)
    {
        Console.WriteLine("App Created");
        WebApplication configured = application.ConfigureEndpoints();
        Console.WriteLine("Endpoints configured");

        return configured.RunAsync();
    }
}
