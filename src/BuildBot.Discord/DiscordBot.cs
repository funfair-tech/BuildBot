using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord
{
    public class DiscordBot : IDiscordBot
    {
        private readonly DiscordBotConfiguration _botConfiguration;
        private readonly DiscordSocketClient _client;
        private readonly ILogger _logger;

        public DiscordBot(DiscordBotConfiguration botConfiguration, ILogger logger)
        {
            this._logger = logger;
            this._client = new DiscordSocketClient();
            this._botConfiguration = botConfiguration;

            this._client.Log += this.LogAsync;
        }

        public async Task PublishAsync(string message)
        {
            SocketTextChannel socketTextChannel = this.GetChannel();

            if (socketTextChannel != null)
            {
                using (socketTextChannel.EnterTypingState())
                {
                    await socketTextChannel.SendMessageAsync(message);
                }
            }
        }

        public async Task PublishAsync(EmbedBuilder builder)
        {
            EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder
                                               {
                                                   Name = "FunFair BuildBot", Url = "https://funfair.io", IconUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png"
                                               };
            builder.WithAuthor(authorBuilder);

            SocketTextChannel socketTextChannel = this.GetChannel();

            if (socketTextChannel != null)
            {
                using (socketTextChannel.EnterTypingState())
                {
                    await socketTextChannel.SendMessageAsync(string.Empty, embed: builder);
                }
            }
        }

        private SocketTextChannel GetChannel()
        {
            SocketGuild guild = this._client.Guilds.FirstOrDefault(g => g.Name == this._botConfiguration.Server);

            return guild?.TextChannels.FirstOrDefault(c => c.Name == this._botConfiguration.Channel);
        }

        private Task LogAsync(LogMessage arg)
        {
            switch (arg.Severity)
            {
                case LogSeverity.Debug:
                {
                    this._logger.LogDebug(arg.Message);

                    break;
                }

                case LogSeverity.Verbose:
                {
                    this._logger.LogInformation(arg.Message);

                    break;
                }

                case LogSeverity.Info:
                {
                    this._logger.LogInformation(arg.Message);

                    break;
                }

                case LogSeverity.Warning:
                {
                    this._logger.LogWarning(arg.Message);

                    break;
                }

                case LogSeverity.Error:
                {
                    if (arg.Exception != null)
                    {
                        this._logger.LogError(new EventId(arg.Exception.HResult), arg.Message, arg.Exception);
                    }
                    else
                    {
                        this._logger.LogError(arg.Message);
                    }

                    break;
                }

                case LogSeverity.Critical:
                {
                    this._logger.LogCritical(arg.Message);

                    break;
                }
            }

            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            // login
            await this._client.LoginAsync(TokenType.Bot, this._botConfiguration.Token);

            // and start
            await this._client.StartAsync();
        }

        public Task StopAsync()
        {
            // and logout
            return this._client.LogoutAsync();
        }
    }
}