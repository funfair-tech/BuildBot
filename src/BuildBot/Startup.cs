using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;
using BuildBot.Discord;
using BuildBot.Discord.Publishers;
using BuildBot.Discord.Publishers.GitHub;
using BuildBot.Discord.Publishers.Octopus;
using BuildBot.Helpers;
using BuildBot.Json;
using BuildBot.Middleware;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;
using BuildBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octopus.Client;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Sensitive;

namespace BuildBot;

public sealed class Startup
{
    public Startup(IHostEnvironment env)
    {
        env.ContentRootFileProvider = new NullFileProvider();

        Log.Logger = CreateLogger();

        this.Configuration = new ConfigurationBuilder().SetBasePath(ApplicationConfigLocator.ConfigurationFilesPath)
                                                       .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: false)
                                                       .AddJsonFile(path: "appsettings-local.json", optional: true, reloadOnChange: false)
                                                       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
                                                       .AddEnvironmentVariables()
                                                       .Build();
    }

    public IConfigurationRoot Configuration { get; }

    private static Logger CreateLogger()
    {
        return new LoggerConfiguration().Enrich.FromLogContext()
                                        .Enrich.WithSensitiveDataMasking(options =>
                                                                         {
                                                                             options.Mode = MaskingMode.Globally;
                                                                             options.MaskingOperators = new()
                                                                                                        {
                                                                                                            new EmailAddressMaskingOperator(),
                                                                                                            new CreditCardMaskingOperator(),
                                                                                                            new IbanMaskingOperator()

                                                                                                            // need to find a sane way of adding these
                                                                                                        };
                                                                             options.MaskValue = "**MASKED*";
                                                                         })
                                        .Enrich.WithDemystifiedStackTraces()
                                        .Enrich.FromLogContext()
                                        .Enrich.WithMachineName()
                                        .Enrich.WithProcessId()
                                        .Enrich.WithThreadId()
                                        .WriteTo.Console()
                                        .CreateLogger();
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        static void ConfigureResponseCompression(ResponseCompressionOptions options)
        {
            options.EnableForHttps = true;

            // Explicitly enable Gzip
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            string[] customMimeTypes =
            {
                "image/svg+xml"
            };

            // Add Custom mime types
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(customMimeTypes);
        }

        DiscordBotConfiguration botConfiguration = this.LoadDiscordConfig();
        OctopusServerEndpoint ose = this.LoadOctopusServerEndpoint();

        services.AddLogging()
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
                .AddRouting()

                // Add framework services
                .Configure<GzipCompressionProviderOptions>(configureOptions: options => options.Level = CompressionLevel.Fastest)
                .Configure<BrotliCompressionProviderOptions>(configureOptions: options => options.Level = CompressionLevel.Fastest)
                .AddResponseCompression(configureOptions: ConfigureResponseCompression)
                .AddMvc()
                .AddMvcOptions(setupAction: _ =>
                                            {
                                                // Note Additional ModelMetadata providers that require DI are enabled elsewhere
                                            })
                .AddJsonOptions(configure: options => JsonSerialiser.Configure(options.JsonSerializerOptions));
    }

    private OctopusServerEndpoint LoadOctopusServerEndpoint()
    {
        string uri = this.Configuration[@"ServerOctopus:Url"] ?? string.Empty;
        string apiKey = this.Configuration[@"ServerOctopus:ApiKey"] ?? string.Empty;

        OctopusServerEndpoint ose = new(octopusServerAddress: uri, apiKey: apiKey);

        return ose;
    }

    private DiscordBotConfiguration LoadDiscordConfig()
    {
        return new(server: this.Configuration[@"Discord:Server"] ?? string.Empty,
                   channel: this.Configuration[@"Discord:Channel"] ?? string.Empty,
                   releaseChannel: this.Configuration[@"Discord:ReleaseChannel"] ?? string.Empty,
                   token: this.Configuration[@"Discord:Token"] ?? string.Empty);
    }

    [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Global", Justification = "Called by the runtime")]
    [SuppressMessage(category: "Microsoft.Performance", checkId: "CA1822:MarkMembersAsStatic", Justification = "Can't be static as called by the runtime.")]
    [SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091: Make static", Justification = "Called by the runtime")]
    [SuppressMessage(category: "SmartAnalyzers.CSharpExtensions.Annotations", checkId: "CSE007: Handle dispose correctly", Justification = "Called by the runtime")]
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostApplicationLifetime applicationLifeTime)
    {
        loggerFactory.AddSerilog();
        applicationLifeTime.ApplicationStopping.Register(Log.CloseAndFlush);

        app.UseHttpLogging();

        app.UseResponseCompression()
           .UseRouting()
           .UseMiddleware<GitHubMiddleware>()
           .UseEndpoints(configure: endpoints => endpoints.MapControllers());
    }
}