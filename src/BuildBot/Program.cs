using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Helpers;
using Credfeto.Docker.HealthCheck.Http.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildBot;

public static class Program
{
    private const int MIN_THREADS = 32;

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

        if (SelfTest.IsSelfTest(args))
        {
            return await SelfTest.RunAsync(
                args: args,
                configPath: ApplicationConfigLocator.ConfigurationFilesPath,
                output: Console.Out,
                cancellationToken: CancellationToken.None
            );
        }

        return await RunServerAsync(
            args: args,
            configPath: ApplicationConfigLocator.ConfigurationFilesPath,
            cancellationToken: CancellationToken.None
        );
    }

    public static async ValueTask<int> RunServerAsync(
        IReadOnlyList<string> args,
        string configPath,
        CancellationToken cancellationToken
    )
    {
        StartupBanner.Show();

        ServerStartup.SetThreads(MIN_THREADS);

        try
        {
            await RunAppAsync(args: args, configPath: configPath, cancellationToken: cancellationToken);

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred:");
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);

            return 1;
        }
    }

    private static async ValueTask RunAppAsync(
        IReadOnlyList<string> args,
        string configPath,
        CancellationToken cancellationToken
    )
    {
        await using (WebApplication app = ServerStartup.CreateApp(args: args, configPath: configPath))
        {
            await RunAsync(application: app, cancellationToken: cancellationToken);
        }
    }

    private static Task RunAsync(WebApplication application, CancellationToken cancellationToken)
    {
        Console.WriteLine("App Created");
        WebApplication configured = application.ConfigureEndpoints();
        Console.WriteLine("Endpoints configured");

        return configured.RunAsync(cancellationToken);
    }
}
