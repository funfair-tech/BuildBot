using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

    public static IHost CreateWebHost<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TStartup>(
        string[] args,
        int httpPort,
        int httpsPort,
        int h2Port,
        string configurationFiledPath)
        where TStartup : class
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

        return Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(webHostBuilder =>
                                             {
                                                 webHostBuilder.UseKestrel(options: options => SetKestrelOptions(options: options,
                                                                                                                 httpPort: httpPort,
                                                                                                                 httpsPort: httpsPort,
                                                                                                                 h2Port: h2Port,
                                                                                                                 configurationFiledPath: configurationFiledPath))
                                                               .UseSetting(key: WebHostDefaults.SuppressStatusMessagesKey, value: "True")
                                                               .UseStartup<TStartup>()
                                                               .ConfigureLogging((_, logger) => logger.ClearProviders());
                                             })
                   .UseWindowsService()
                   .UseSystemd()
                   .Build();
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
}