using BuildBot.Discord;
using BuildBot.Discord.Publishers;
using BuildBot.Discord.Publishers.GitHub;
using BuildBot.Middleware;
using BuildBot.ServiceModel.GitHub;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BuildBot
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Console()
              .CreateLogger();

            this._loggerFactory = loggerFactory;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // set up an ILogger
            Microsoft.Extensions.Logging.ILogger logger = this._loggerFactory.CreateLogger("BuildBot");
            services.AddSingleton(logger);

            DiscordBotConfiguration botConfiguration = DiscordBotConfiguration.Load("buildbot-config.json");
            DiscordBot bot = new DiscordBot(botConfiguration, logger);

            // waiting a Task is normally a big no no because of deadlocks, but we're in a start up task here so it should be ok
            bot.Start().Wait();

            // register the bot for DI
            services.AddSingleton<IDiscordBot>(bot);

            // register publishers
            services.AddSingleton<IPublisher<Push>, PushPublisher>();

            // Add framework services
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();

            app.UseMiddleware<GitHubMiddleware>();
            app.UseMvc();
        }
    }
}
