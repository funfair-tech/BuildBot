using System;
using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord.Services;

public interface IDiscordRawClient
{
    LoginState LoginState { get; }

    IDiscordChannel? FindChannel(string serverName, string channelName);

    Task LoginAsync(TokenType tokenType, string token);

    Task StartAsync();

    Task StopAsync();

    Task LogoutAsync();

    Task SetGameAsync(string name, string? streamUrl, ActivityType type);

    void RegisterLogHandler(Func<LogMessage, Task> handler);
}
