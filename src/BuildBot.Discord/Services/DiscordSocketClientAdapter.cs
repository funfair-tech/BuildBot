using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BuildBot.Discord.Services;

internal sealed class DiscordSocketClientAdapter : IDiscordRawClient
{
    private readonly DiscordSocketClient _client;

    public DiscordSocketClientAdapter()
    {
        this._client = new(
            new()
            {
                GatewayIntents =
                    GatewayIntents.Guilds | GatewayIntents.GuildMessageTyping | GatewayIntents.GuildMessages,
            }
        );
    }

    public LoginState LoginState => this._client.LoginState;

    public IDiscordChannel? FindChannel(string serverName, string channelName)
    {
        SocketGuild? guild = this._client.Guilds.FirstOrDefault(predicate: g =>
            StringComparer.Ordinal.Equals(x: g.Name, y: serverName)
        );

        SocketTextChannel? textChannel = guild?.TextChannels.FirstOrDefault(predicate: c =>
            StringComparer.OrdinalIgnoreCase.Equals(x: c.Name, y: channelName)
        );

        return textChannel is null ? null : new DiscordChannelAdapter(textChannel);
    }

    public Task LoginAsync(TokenType tokenType, string token)
    {
        return this._client.LoginAsync(tokenType: tokenType, token: token);
    }

    public Task StartAsync()
    {
        return this._client.StartAsync();
    }

    public Task StopAsync()
    {
        return this._client.StopAsync();
    }

    public Task LogoutAsync()
    {
        return this._client.LogoutAsync();
    }

    public Task SetGameAsync(string name, string? streamUrl, ActivityType type)
    {
        return this._client.SetGameAsync(name: name, streamUrl: streamUrl, type: type);
    }

    public void RegisterLogHandler(Func<LogMessage, Task> handler)
    {
        this._client.Log += handler;
    }
}
