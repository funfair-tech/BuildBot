using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace BuildBot.Discord
{
    public class DiscordBot : IDiscordBot
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger _logger;
        private readonly DiscordBotConfiguration _botConfiguration;

        public DiscordBot(DiscordBotConfiguration botConfiguration, ILogger logger)
        {
            this._logger = logger;
            this._client = new DiscordSocketClient();
            this._botConfiguration = botConfiguration;

            this._client.Log += this.Log;
        }

        private SocketTextChannel GetChannel()
        {
            SocketGuild guild = this._client.Guilds.FirstOrDefault(g => g.Name == this._botConfiguration.Server);
            return guild != null ? guild.TextChannels.FirstOrDefault(c => c.Name == this._botConfiguration.Channel) : null;
        }

        public async Task Publish(string message)
        {
            SocketTextChannel socketTextChannel = this.GetChannel();
            if (socketTextChannel != null)
            {
                await socketTextChannel.SendMessageAsync(message);
            }
        }

        public async Task Publish(Embed embed)
        {
            SocketTextChannel socketTextChannel = this.GetChannel();
            if (socketTextChannel != null)
            {
                await socketTextChannel.SendMessageAsync(string.Empty, embed: embed);
            }
        }

        private async Task Log(LogMessage arg)
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

            await Task.CompletedTask;
        }

        public async Task Start()
        {
            // login
            await this._client.LoginAsync(TokenType.Bot, this._botConfiguration.Token);

            // and start
            await this._client.StartAsync();
        }

        public async Task Stop()
        {
            // and logout
            await this._client.LogoutAsync();
        }
    }
}
