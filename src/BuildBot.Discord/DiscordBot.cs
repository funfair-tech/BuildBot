using System;
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

        public async Task PublishAsync(EmbedBuilder builder)
        {
            SocketTextChannel? socketTextChannel = this.GetChannel(this._botConfiguration.Channel);

            if (socketTextChannel == null)
            {
                return;
            }

            await PublishCommonAsync(builder, socketTextChannel);
        }

        public async Task PublishToReleaseChannelAsync(EmbedBuilder builder)
        {
            SocketTextChannel? socketTextChannel = this.GetChannel(this._botConfiguration.ReleaseChannel ?? this._botConfiguration.Channel);

            if (socketTextChannel == null)
            {
                return;
            }

            await PublishCommonAsync(builder, socketTextChannel);
        }

        private static async Task PublishCommonAsync(EmbedBuilder builder, SocketTextChannel socketTextChannel)
        {
            using (socketTextChannel.EnterTypingState())
            {
                EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder
                                                   {
                                                       Name = "FunFair BuildBot",
                                                       Url = "https://funfair.io",
                                                       IconUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png"
                                                   };
                builder.WithAuthor(authorBuilder);

                await socketTextChannel.SendMessageAsync(string.Empty, embed: builder.Build());
            }
        }

        private SocketTextChannel? GetChannel(string channelName)
        {
            SocketGuild guild = this._client.Guilds.FirstOrDefault(predicate: g => g.Name == this._botConfiguration.Server);

            return guild?.TextChannels.FirstOrDefault(predicate: c => StringComparer.InvariantCultureIgnoreCase.Equals(c.Name, channelName));
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