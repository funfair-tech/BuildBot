using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using BuildBot.CloudFormation;
using BuildBot.Discord;
using BuildBot.GitHub;
using BuildBot.Health;
using BuildBot.Json;
using BuildBot.Watchtower;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;

namespace BuildBot.Helpers;

public static class ServerStartup
{
    public static void SetThreads(int minThreads)
    {
        ThreadPool.GetMinThreads(out int minWorker, out int minIoc);
        Console.WriteLine($"Min worker threads {minWorker}, Min IOC threads {minIoc}");

        if (minWorker < minThreads && minIoc < minThreads)
        {
            Console.WriteLine($"Setting min worker threads {minThreads}, Min IOC threads {minThreads}");
            ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);
        }
        else if (minWorker < minThreads)
        {
            Console.WriteLine($"Setting min worker threads {minThreads}, Min IOC threads {minIoc}");
            ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minIoc);
        }
        else if (minIoc < minThreads)
        {
            Console.WriteLine($"Setting min worker threads {minWorker}, Min IOC threads {minThreads}");
            ThreadPool.SetMinThreads(workerThreads: minWorker, completionPortThreads: minThreads);
        }

        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIoc);
        Console.WriteLine($"Max worker threads {maxWorker}, Max IOC threads {maxIoc}");
    }

    public static WebApplication CreateApp(
        IReadOnlyList<string> args,
        string? configPath = null,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null,
        bool validateServices = false
    )
    {
        const int httpPort = 8080;
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder([.. args]);

        string configFilesPath = configPath ?? ApplicationConfigLocator.ConfigurationFilesPath;
        IConfigurationRoot config = LoadConfiguration(
            configPath: configFilesPath,
            overrides: configurationOverrides ?? FrozenDictionary<string, string?>.Empty
        );

        builder.Host.UseWindowsService().UseSystemd();

        if (validateServices)
        {
            builder.Host.UseDefaultServiceProvider(static options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });
        }
        builder
            .WebHost.UseKestrel(options: static options => SetKestrelOptions(options: options, httpPort: httpPort))
            .UseSetting(key: WebHostDefaults.SuppressStatusMessagesKey, value: "True")
            .ConfigureLogging((_, logger) => ConfigureLogging(logger));

        builder
            .Services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.TypeInfoResolverChain.Insert(index: 0, item: AppSerializationContext.Default)
            )
            .AddDiscord(config)
            .AddCloudFormation(config)
            .AddGitHub(config)
            .AddWatchtower()
            .AddStatus()
            .AddMediator();

        return builder.Build();
    }

    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2000:DisposeObjectsBeforeLosingScope",
        Justification = "Lives for program lifetime"
    )]
    private static void ConfigureLogging(ILoggingBuilder logger)
    {
        logger.ClearProviders().AddSerilog(CreateLogger(), dispose: true);
    }

    private static Logger CreateLogger()
    {
        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty(name: "ServerVersion", value: VersionInformation.Version)
            .Enrich.WithProperty(name: "ProcessName", value: VersionInformation.Product)
            .WriteToDebuggerAwareOutput()
            .CreateLogger();
    }

    private static LoggerConfiguration WriteToDebuggerAwareOutput(this LoggerConfiguration configuration)
    {
        LoggerSinkConfiguration writeTo = configuration.WriteTo;

        return Debugger.IsAttached ? writeTo.Debug() : writeTo.Console();
    }

    private static IConfigurationRoot LoadConfiguration(
        string configPath,
        IReadOnlyDictionary<string, string?> overrides
    )
    {
        return new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile(path: "appsettings-local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(overrides)
            .Build();
    }

    private static void SetH1ListenOptions(ListenOptions listenOptions)
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    }

    private static void SetKestrelOptions(KestrelServerOptions options, int httpPort)
    {
        options.DisableStringReuse = false;
        options.AllowSynchronousIO = false;

        options.AddServerHeader = false;
        options.Limits.MinResponseDataRate = null;
        options.Limits.MinRequestBodyDataRate = null;

        Console.WriteLine($"Listening on HTTP port: {httpPort}");
        options.Listen(address: IPAddress.Any, port: httpPort, configure: SetH1ListenOptions);
    }
}
