using System.Diagnostics.CodeAnalysis;
using System.IO;
using BuildBot.Discord;
using BuildBot.Discord.Publishers;
using BuildBot.Discord.Publishers.GitHub;
using BuildBot.Discord.Publishers.Octopus;
using BuildBot.Middleware;
using BuildBot.ServiceModel.GitHub;
using BuildBot.ServiceModel.Octopus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octopus.Client;
using Serilog;

namespace BuildBot
{
    public sealed class Startup
    {
        public Startup(IHostEnvironment env)
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
                                                  .WriteTo.Console()
                                                  .CreateLogger();

            IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(ApplicationConfig.ConfigurationFilesPath)
                                                                      .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                                                                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                                                                      .AddEnvironmentVariables();

            this.Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            string configFile = Path.Combine(path1: ApplicationConfig.ConfigurationFilesPath, path2: "buildbot-config.json");
            DiscordBotConfiguration botConfiguration = DiscordBotConfiguration.Load(configFile);

            services.AddSingleton(botConfiguration);

            // register the bot for DI
            services.AddSingleton<IDiscordBot, DiscordBot>();
            services.AddSingleton(x => (DiscordBot) x.GetRequiredService<IDiscordBot>());

            // register publishers
            services.AddSingleton<IPublisher<Push>, PushPublisher>();
            services.AddSingleton<IPublisher<Status>, StatusPublisher>();

            services.AddSingleton<IPublisher<Deploy>, DeployPublisher>();

            services.AddSingleton<IOctopusClientFactory, OctopusClientFactory>();

            string uri = this.Configuration.GetValue<string>(key: @"ServerOctopus:Url");
            string apiKey = this.Configuration.GetValue<string>(key: @"ServerOctopus:ApiKey");

            OctopusServerEndpoint ose = new(octopusServerAddress: uri, apiKey: apiKey);

            services.AddSingleton(ose);

            // Add framework services
            services.AddMvc();
        }

        /// <summary>
        ///     Configures the HTTP request pipeline
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="loggerFactory">Logging factory.</param>
        /// <param name="applicationLifeTime">The lifetime of the application.</param>
        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>


        [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Global", Justification = "TODO: Review")]
        [SuppressMessage(category: "Microsoft.Performance", checkId: "CA1822:MarkMembersAsStatic", Justification = "Can't be static as called by the runtime.")]
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostApplicationLifetime applicationLifeTime)
        {
            loggerFactory.AddSerilog();
            applicationLifeTime.ApplicationStopping.Register(Log.CloseAndFlush);

            app.UseRouting()
               .UseMiddleware<GitHubMiddleware>()
               .UseEndpoints(configure: endpoints => { endpoints.MapControllers(); });
        }
    }
}

