using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Services.LoggingExtensions;
using BuildBot.ServiceModel.ComponentStatus;
using Discord;
using Microsoft.Extensions.Logging;

namespace BuildBot.Discord.Services;

public sealed class DiscordBot : IDiscordBot, IComponentStatus
{
    private static readonly TimeSpan TypingDelay = TimeSpan.FromSeconds(2);
    private readonly DiscordBotConfiguration _botConfiguration;
    private readonly IDiscordRawClient _client;
    private readonly ILogger<DiscordBot> _logger;
    private readonly SemaphoreSlim _semaphore = new(1);

    public DiscordBot(IDiscordRawClient client, DiscordBotConfiguration botConfiguration, ILogger<DiscordBot> logger)
    {
        this._client = client;
        this._botConfiguration = botConfiguration;
        this._logger = logger;
        this._client.RegisterLogHandler(this.LogAsync);
    }

    private static EmbedAuthorBuilder Author { get; } =
        new EmbedAuthorBuilder()
            .WithName("FunFair BuildBot")
            .WithUrl("https://funfair.io")
            .WithIconUrl("https://s2.coinmarketcap.com/static/img/coins/32x32/1757.png");

    public ServiceStatus GetStatus()
    {
        return new(Name: "Discord", this._client.LoginState == LoginState.LoggedIn);
    }

    public async ValueTask PublishAsync(EmbedBuilder builder, CancellationToken cancellationToken)
    {
        IDiscordChannel? channel = this.GetChannel(this._botConfiguration.Channel);

        if (channel is null)
        {
            this._logger.LogDiscordChannelNotFound(
                channelName: this._botConfiguration.Channel,
                serverName: this._botConfiguration.Server
            );

            return;
        }

        await this.PublishCommonAsync(builder: builder, channel: channel, cancellationToken: cancellationToken);
    }

    public async ValueTask PublishToReleaseChannelAsync(EmbedBuilder builder, CancellationToken cancellationToken)
    {
        IDiscordChannel? channel = this.GetChannel(this._botConfiguration.ReleaseChannel);

        if (channel is null)
        {
            this._logger.LogDiscordChannelNotFound(
                channelName: this._botConfiguration.Channel,
                serverName: this._botConfiguration.Server
            );

            return;
        }

        await this.PublishCommonAsync(builder: builder, channel: channel, cancellationToken: cancellationToken);
    }

    private async ValueTask PublishCommonAsync(
        EmbedBuilder builder,
        IDiscordChannel channel,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this._logger.LogSendingMessage(channelName: channel.Name, message: builder.Title ?? string.Empty);

            using (channel.EnterTypingState())
            {
                Embed embed = IncludeStandardParameters(builder);

                (string sentToChannel, string messageContent) = await channel.SendMessageAsync(embed);
                this.LogSent(channelName: sentToChannel, content: messageContent);
                await Task.Delay(delay: TypingDelay, cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception)
        {
            this._logger.FailedToPublishMessage(
                channelName: this._botConfiguration.Channel,
                title: builder.Title ?? string.Empty,
                message: exception.Message,
                exception: exception
            );

            await this.ReconnectAsync(CancellationToken.None);
        }
    }

    private void LogSent(string channelName, string content)
    {
        this._logger.LogSentMessage(channelName: channelName, message: content);
    }

    private static Embed IncludeStandardParameters(EmbedBuilder builder)
    {
        return builder.WithAuthor(Author).Build();
    }

    private IDiscordChannel? GetChannel(string channelName)
    {
        return this._client.FindChannel(serverName: this._botConfiguration.Server, channelName: channelName);
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
        if (arg.Exception is not null)
        {
            this._logger.DiscordCriticalError(message: arg.Message, exception: arg.Exception);
        }
        else
        {
            this._logger.DiscordCriticalError(arg.Message);
        }

        return Task.CompletedTask;
    }

    private Task LogErrorAsync(LogMessage arg)
    {
        if (arg.Exception is not null)
        {
            this._logger.DiscordError(message: arg.Message, exception: arg.Exception);
        }
        else
        {
            this._logger.DiscordError(arg.Message);
        }

        return Task.CompletedTask;
    }

    private Task LogWarningAsync(LogMessage arg)
    {
        this._logger.DiscordWarning(arg.Message);

        return Task.CompletedTask;
    }

    private Task LogInformationAsync(LogMessage arg)
    {
        this._logger.DiscordInfo(arg.Message);

        return Task.CompletedTask;
    }

    private Task LogDebugAsync(LogMessage arg)
    {
        this._logger.DiscordDebug(arg.Message);

        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this._client.LoginAsync(tokenType: TokenType.Bot, token: this._botConfiguration.Token);
        await this._client.StartAsync();
        await this._client.SetGameAsync(name: "GitHub", streamUrl: null, type: ActivityType.Watching);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return this._client.LogoutAsync();
    }

    private async ValueTask ReconnectAsync(CancellationToken cancellationToken)
    {
        await this._semaphore.WaitAsync(cancellationToken);

        try
        {
            if (this._client.LoginState == LoginState.LoggedIn)
            {
                await this._client.LogoutAsync();
                await this._client.StopAsync();
            }

            await this._client.LoginAsync(tokenType: TokenType.Bot, token: this._botConfiguration.Token);
            await this._client.StartAsync();
            await this._client.SetGameAsync(name: "GitHub", streamUrl: null, type: ActivityType.Watching);
        }
        catch (Exception exception)
        {
            this._logger.DiscordError(message: "Failed to reconnect", exception: exception);
        }
        finally
        {
            this._semaphore.Release();
        }
    }
}
