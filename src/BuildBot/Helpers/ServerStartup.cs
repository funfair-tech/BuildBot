using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using BuildBot.Discord;
using BuildBot.Discord.Publishers;
using BuildBot.Discord.Publishers.GitHub;
using BuildBot.Discord.Publishers.Octopus;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;
using BuildBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octopus.Client;

namespace BuildBot.Helpers;

internal static class ServerStartup
{
    private static readonly IReadOnlyList<string> CustomMimeTypes =
    [
        "image/svg+xml"
    ];

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

    public static IHost CreateWebHost(string[] args, int httpPort, int httpsPort, int h2Port, string configurationFiledPath)
    {
        if (httpPort == 0 && httpsPort == 0 && h2Port == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(httpPort), message: "Not listening on any protocol");
        }

        if (httpPort != 0)
        {
            Console.WriteLine($"Listening on http://localhost:{httpPort}/");
        }

        if (httpsPort != 0)
        {
            Console.WriteLine($"Listening on https://localhost:{httpsPort}/");
        }

        if (h2Port != 0)
        {
            Console.WriteLine($"Listening on http://localhost:{h2Port}/ for H2");
        }

        AppContext.SetSwitch(switchName: "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", isEnabled: true);

        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new() { ApplicationName = typeof(Program).FullName, Args = args });
        builder.ConfigureWebHost(httpPort: httpPort, httpsPort: httpsPort, h2Port: h2Port, configurationFiledPath: configurationFiledPath)
               .EnableRunAsService();

        WebApplication app = builder.Build();

        app.MapGet(pattern: "/ping", handler: () => PingPong.Model);

        return app;
    }

    private static WebApplicationBuilder EnableRunAsService(this WebApplicationBuilder builder)

    {
        builder.Host.UseWindowsService()
               .UseSystemd();

        return builder;
    }

    private static WebApplicationBuilder ConfigureWebHost(this WebApplicationBuilder builder, int httpPort, int httpsPort, int h2Port, string configurationFiledPath)

    {
        builder.Configuration.SetBasePath(configurationFiledPath)
               .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: false)
               .AddJsonFile(path: "appsettings-local.json", optional: true, reloadOnChange: false)
               .AddEnvironmentVariables();

        builder.WebHost.UseKestrel(options: options => SetKestrelOptions(options: options, httpPort: httpPort, httpsPort: httpsPort, h2Port: h2Port, configurationFiledPath: configurationFiledPath))
               .UseSetting(key: WebHostDefaults.SuppressStatusMessagesKey, value: "True")
               .ConfigureLogging((_, logger) => logger.ClearProviders());

        DiscordBotConfiguration botConfiguration = builder.Configuration.LoadDiscordConfig();
        OctopusServerEndpoint ose = builder.Configuration.LoadOctopusServerEndpoint();

        builder.Services.AddLogging()
               .AddHttpLogging(options => options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.RequestBody)
               .AddSingleton(botConfiguration)

               // register the bot for DI
               .AddSingleton<IDiscordBot, DiscordBot>()
               .AddSingleton(x => (DiscordBot)x.GetRequiredService<IDiscordBot>())

               // register publishers
               .AddSingleton<IPublisher<Push>, PushPublisher>()
               .AddSingleton<IPublisher<Status>, StatusPublisher>()
               .AddSingleton<IPublisher<Deploy>, DeployPublisher>()
               .AddSingleton<IOctopusClientFactory, OctopusClientFactory>()
               .AddSingleton(ose)
               .AddHostedService<BotService>()
               .Configure<GzipCompressionProviderOptions>(configureOptions: options => options.Level = CompressionLevel.Fastest)
               .Configure<BrotliCompressionProviderOptions>(configureOptions: options => options.Level = CompressionLevel.Fastest)
               .AddResponseCompression(configureOptions: ConfigureResponseCompression);

        //.AddJsonOptions(configure: options => JsonSerialiser.Configure(options.JsonSerializerOptions))

        return builder;
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
            // Enable HTTP3 when it isn't a preview feature
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
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
            options.Listen(address: IPAddress.Any, port: httpsPort, configure: o => SetHttpsListenOptions(listenOptions: o, configurationFiledPath: configurationFiledPath));
        }

        if (h2Port != 0)
        {
            options.Listen(address: IPAddress.Any, port: h2Port, configure: SetH2ListenOptions);
        }

        options.Listen(address: IPAddress.Any, port: httpPort);
    }

    private static void ConfigureResponseCompression(ResponseCompressionOptions options)
    {
        options.EnableForHttps = true;

        // Explicitly enable Gzip
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();

        // Add Custom mime types
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(CustomMimeTypes);
    }

    private static OctopusServerEndpoint LoadOctopusServerEndpoint(this IConfigurationRoot configuration)
    {
        string uri = configuration["ServerOctopus:Url"] ?? string.Empty;
        string apiKey = configuration["ServerOctopus:ApiKey"] ?? string.Empty;

        return new(octopusServerAddress: uri, apiKey: apiKey);
    }

    private static DiscordBotConfiguration LoadDiscordConfig(this IConfigurationRoot configuration)
    {
        return new(server: configuration["Discord:Server"] ?? string.Empty,
                   channel: configuration["Discord:Channel"] ?? string.Empty,
                   releaseChannel: configuration["Discord:ReleaseChannel"] ?? string.Empty,
                   token: configuration["Discord:Token"] ?? string.Empty);
    }
}