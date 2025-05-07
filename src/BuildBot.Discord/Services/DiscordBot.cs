using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services.LoggingExtensions;
using BuildBot.ServiceModel.ComponentStatus;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services;

public sealed class DiscordBot : IDiscordBot, IComponentStatus
{
    private static readonly TimeSpan TypingDelay = TimeSpan.FromSeconds(2);
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

    private static EmbedAuthorBuilder Author { get; } =
        new EmbedAuthorBuilder()
            .WithName("FunFair BuildBot")
            .WithUrl("https://funfair.io")
            .WithIconUrl("https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png");

    public ServiceStatus GetStatus()
    {
        return new ServiceStatus(Name: "Discord", Ok: this._client.LoginState == LoginState.LoggedIn);
    }

    public async ValueTask PublishAsync(EmbedBuilder builder, CancellationToken cancellationToken)
    {
        SocketTextChannel? socketTextChannel = this.GetChannel(this._botConfiguration.Channel);

        if (socketTextChannel is null)
        {
            this._logger.LogDiscordChannelNotFound(
                channelName: this._botConfiguration.Channel,
                serverName: this._botConfiguration.Server
            );

            return;
        }

        await this.PublishCommonAsync(
            builder: builder,
            socketTextChannel: socketTextChannel,
            cancellationToken: cancellationToken
        );
    }

    public async ValueTask PublishToReleaseChannelAsync(EmbedBuilder builder, CancellationToken cancellationToken)
    {
        SocketTextChannel? socketTextChannel = this.GetChannel(this._botConfiguration.ReleaseChannel);

        if (socketTextChannel is null)
        {
            this._logger.LogDiscordChannelNotFound(
                channelName: this._botConfiguration.Channel,
                serverName: this._botConfiguration.Server
            );

            return;
        }

        await this.PublishCommonAsync(
            builder: builder,
            socketTextChannel: socketTextChannel,
            cancellationToken: cancellationToken
        );
    }

    private async ValueTask PublishCommonAsync(
        EmbedBuilder builder,
        SocketTextChannel socketTextChannel,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogSendingMessage(channelName: socketTextChannel.Name, message: builder.Title);

            using (socketTextChannel.EnterTypingState())
            {
                Embed embed = IncludeStandardParameters(builder);

                RestUserMessage msg = await socketTextChannel.SendMessageAsync(text: string.Empty, embed: embed);
                this.LogSent(msg);
                await Task.Delay(delay: TypingDelay, cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception)
        {
            this._logger.FailedToPublishMessage(
                channelName: this._botConfiguration.Channel,
                title: builder.Title,
                message: exception.Message,
                exception: exception
            );

            throw;
        }
    }

    private void LogSent(RestUserMessage msg)
    {
        this._logger.LogSentMessage(channelName: msg.Channel.Name, message: msg.CleanContent);
    }

    private static Embed IncludeStandardParameters(EmbedBuilder builder)
    {
        return builder.WithAuthor(Author).Build();
    }

    private SocketTextChannel? GetChannel(string channelName)
    {
        SocketGuild? guild = this._client.Guilds.FirstOrDefault(predicate: g =>
            StringComparer.Ordinal.Equals(x: g.Name, y: this._botConfiguration.Server)
        );

        return guild?.TextChannels.FirstOrDefault(predicate: c =>
            StringComparer.OrdinalIgnoreCase.Equals(x: c.Name, y: channelName)
        );
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
            _ => this.LogCriticalAsync(arg),
        };
    }

    private Task LogCriticalAsync(LogMessage arg)
    {
        this._logger.LogCritical(arg.Message);

        return Task.CompletedTask;
    }

    private Task LogErrorAsync(LogMessage arg)
    {
        if (arg.Exception is not null)
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

        await this._client.SetGameAsync(name: "GitHub", streamUrl: null, type: ActivityType.Watching);
    }

    public Task StopAsync()
    {
        // and logout
        return this._client.LogoutAsync();
    }
}
