using System.Diagnostics.CodeAnalysis;
using BuildBot.Discord;
using BuildBot.Discord.Publishers;
using BuildBot.Discord.Publishers.GitHub;
using BuildBot.Middleware;
using BuildBot.ServiceModel.GitHub;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BuildBot
{
    public sealed class Startup
    {
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IHostEnvironment env, ILoggerFactory loggerFactory)
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
                                                  .WriteTo.Console()
                                                  .CreateLogger();

            this._loggerFactory = loggerFactory;

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
            // set up an ILogger
            ILogger logger = this._loggerFactory.CreateLogger(categoryName: "BuildBot");
            services.AddSingleton(logger);

            DiscordBot bot = ConfigureBot(logger);

            // register the bot for DI
            services.AddSingleton<IDiscordBot>(bot);

            // register publishers
            services.AddSingleton<IPublisher<Push>, PushPublisher>();
            services.AddSingleton<IPublisher<Status>, StatusPublisher>();

            // Add framework services
            services.AddMvc();
        }

        [SuppressMessage(category: "Threading", checkId: "VSTHRD002:Don't do synchronous waits", Justification = "This is a startup task")]
        private static DiscordBot ConfigureBot(ILogger logger)
        {
            DiscordBotConfiguration botConfiguration = DiscordBotConfiguration.Load(jsonFile: "buildbot-config.json");
            DiscordBot bot = new DiscordBot(botConfiguration, logger);

            // waiting a Task is normally a big no no because of deadlocks, but we're in a start up task here so it should be ok
            bot.StartAsync()
               .Wait();

            return bot;
        }

        /// <summary>
        ///     Configures the HTTP request pipeline
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="loggerFactory">Logging factory.</param>
        /// <param name="applicationLifeTime">The lifetime of the application.</param>
        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>

        // ReSharper disable once UnusedMember.Global
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