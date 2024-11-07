using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using BuildBot.CloudFormation;
using BuildBot.CloudFormation.Configuration;
using BuildBot.Discord;
using BuildBot.GitHub;
using BuildBot.Json;
using BuildBot.Health;
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

internal static class ServerStartup
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

    public static WebApplication CreateApp(string[] args)
    {
        const int httpPort = 8080;
        const int httpsPort = 0;
        const int h2Port = 0;
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

        string configPath = ApplicationConfigLocator.ConfigurationFilesPath;

        IConfigurationRoot config = LoadConfiguration(configPath);

        DiscordBotConfiguration discordConfig = LoadDiscordConfig(config);
        SnsNotificationOptions snsConfiguration = LoadSnsNotificationConfig(config);

        builder.Host.UseWindowsService()
               .UseSystemd();
        builder.WebHost.UseKestrel(options: options => SetKestrelOptions(options: options, httpPort: httpPort, httpsPort: httpsPort, h2Port: h2Port, configurationFiledPath: configPath))
               .UseSetting(key: WebHostDefaults.SuppressStatusMessagesKey, value: "True")
               .ConfigureLogging((_, logger) => ConfigureLogging(logger));

        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(index: 0, item: AppSerializationContext.Default))
               .AddDiscord(discordConfig)
               .AddCloudFormation(snsConfiguration)
               .AddGitHub()
               .AddStatus()
               .AddMediator();

        return builder.Build();
    }

    private static SnsNotificationOptions LoadSnsNotificationConfig(IConfigurationRoot configuration)
    {
        string topicArn = configuration["CloudFormation:TopicArn"] ?? string.Empty;
        string region = configuration["CloudFormation:Aws:Region"] ?? string.Empty;
        string accessKeyId = configuration["CloudFormation:Aws:AccessKeyId"] ?? string.Empty;
        string secretKey = configuration["CloudFormation:Aws:SecretKey"] ?? string.Empty;

        return new(TopicArn: topicArn, Region: region, AccessKey: accessKeyId, SecretKey: secretKey);
    }

    [SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Lives for program lifetime")]
    [SuppressMessage(category: "SmartAnalyzers.CSharpExtensions.Annotations", checkId: "CSE007:DisposeObjectsBeforeLosingScope", Justification = "Lives for program lifetime")]
    private static void ConfigureLogging(ILoggingBuilder logger)
    {
        logger.ClearProviders()
              .AddSerilog(CreateLogger(), dispose: true);
    }

    private static Logger CreateLogger()
    {
        return new LoggerConfiguration().Enrich.WithDemystifiedStackTraces()
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

        return Debugger.IsAttached
            ? writeTo.Debug()
            : writeTo.Console();
    }

    private static IConfigurationRoot LoadConfiguration(string configPath)
    {
        return new ConfigurationBuilder().SetBasePath(configPath)
                                         .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: false)
                                         .AddJsonFile(path: "appsettings-local.json", optional: true, reloadOnChange: false)
                                         .AddEnvironmentVariables()
                                         .Build();
    }

    private static DiscordBotConfiguration LoadDiscordConfig(IConfigurationRoot configuration)
    {
        return new(server: configuration["Discord:Server"] ?? string.Empty,
                   channel: configuration["Discord:Channel"] ?? string.Empty,
                   releaseChannel: configuration["Discord:ReleaseChannel"] ?? string.Empty,
                   token: configuration["Discord:Token"] ?? string.Empty);
    }

    private static void SetH1ListenOptions(ListenOptions listenOptions)
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    }

    private static void SetH2ListenOptions(ListenOptions listenOptions)
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    }

    private static void SetHttpsListenOptions(ListenOptions listenOptions, string configurationFiledPath)
    {
        string certFile = Path.Combine(path1: configurationFiledPath, path2: "server.pfx");

        if (!File.Exists(certFile))
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps();
        }
        else
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
            listenOptions.UseHttps(fileName: certFile);
        }
    }

    private static void SetKestrelOptions(KestrelServerOptions options, int httpPort, int httpsPort, int h2Port, string configurationFiledPath)
    {
        options.DisableStringReuse = false;
        options.AllowSynchronousIO = false;

        options.AddServerHeader = false;
        options.Limits.MinResponseDataRate = null;
        options.Limits.MinRequestBodyDataRate = null;

        if (httpsPort != 0)
        {
            Console.WriteLine($"Listening on HTTPS port: {httpsPort}");
            options.Listen(address: IPAddress.Any, port: httpsPort, configure: o => SetHttpsListenOptions(listenOptions: o, configurationFiledPath: configurationFiledPath));
        }

        if (h2Port != 0)
        {
            Console.WriteLine($"Listening on H2 port: {h2Port}");
            options.Listen(address: IPAddress.Any, port: h2Port, configure: SetH2ListenOptions);
        }

        Console.WriteLine($"Listening on HTTP port: {httpPort}");
        options.Listen(address: IPAddress.Any, port: httpPort, configure: SetH1ListenOptions);
    }
}