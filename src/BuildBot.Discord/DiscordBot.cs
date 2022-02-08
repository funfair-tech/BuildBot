using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord;

public sealed class DiscordBot : IDiscordBot
{
    private readonly DiscordBotConfiguration _botConfiguration;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordBot> _logger;

    public DiscordBot(DiscordBotConfiguration botConfiguration, ILogger<DiscordBot> logger)
    {
        this._logger = logger;
        this._client = new();
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

        await PublishCommonAsync(builder: builder, socketTextChannel: socketTextChannel);
    }

    public async Task PublishToReleaseChannelAsync(EmbedBuilder builder)
    {
        SocketTextChannel? socketTextChannel = this.GetChannel(this._botConfiguration.ReleaseChannel);

        if (socketTextChannel == null)
        {
            return;
        }

        await PublishCommonAsync(builder: builder, socketTextChannel: socketTextChannel);
    }

    private static async Task PublishCommonAsync(EmbedBuilder builder, SocketTextChannel socketTextChannel)
    {
        using (socketTextChannel.EnterTypingState())
        {
            EmbedAuthorBuilder authorBuilder = new()
                                               {
                                                   Name = "FunFair BuildBot", Url = "https://funfair.io", IconUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png"
                                               };
            builder.WithAuthor(authorBuilder);

            await socketTextChannel.SendMessageAsync(text: string.Empty, embed: builder.Build());
        }
    }

    private SocketTextChannel? GetChannel(string channelName)
    {
        SocketGuild? guild = this._client.Guilds.FirstOrDefault(predicate: g => g.Name == this._botConfiguration.Server);

        return guild?.TextChannels.FirstOrDefault(predicate: c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c.Name, y: channelName));
    }

    private Task LogAsync(LogMessage arg)
    {
        return arg.Severity switch
        {
            LogSeverity.Debug => this.LogDebugAsync(arg),
            LogSeverity.Verbose => this.LogInformationAsync(arg),
            LogSeverity.Info => this.LogInformationAsync(arg),
            LogSeverity.Warning => this.LogWarningAsync(arg),
            LogSeverity.Error => this.LogErrorAsync(arg),
            LogSeverity.Critical => this.LogCriticalAsync(arg),
            _ => this.LogCriticalAsync(arg)
        };
    }

    private Task LogCriticalAsync(LogMessage arg)
    {
        this._logger.LogCritical(arg.Message);

        return Task.CompletedTask;
    }

    private Task LogErrorAsync(LogMessage arg)
    {
        if (arg.Exception != null)
        {
            this._logger.LogError(new(arg.Exception.HResult), exception: arg.Exception, message: arg.Message);
        }
        else
        {
            this._logger.LogError(arg.Message);
        }

        return Task.CompletedTask;
    }

    private Task LogWarningAsync(LogMessage arg)
    {
        this._logger.LogWarning(arg.Message);

        return Task.CompletedTask;
    }

    private Task LogInformationAsync(LogMessage arg)
    {
        this._logger.LogInformation(arg.Message);

        return Task.CompletedTask;
    }

    private Task LogDebugAsync(LogMessage arg)
    {
        this._logger.LogDebug(arg.Message);

        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        // login
        await this._client.LoginAsync(tokenType: TokenType.Bot, token: this._botConfiguration.Token);

        // and start
        await this._client.StartAsync();
    }

    public Task StopAsync()
    {
        // and logout
        return this._client.LogoutAsync();
    }
}