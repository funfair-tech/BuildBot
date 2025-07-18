using System.Diagnostics;

namespace BuildBot.Discord;

[DebuggerDisplay("Server: {Server}, Channel: {Channel}, Release Channel: {ReleaseChannel}")]
public sealed class DiscordBotConfiguration
{
    public DiscordBotConfiguration(string token, string server, string channel, string releaseChannel)
    {
        this.Token = token;
        this.Server = server;
        this.Channel = channel;
        this.ReleaseChannel = releaseChannel;
    }

    public string Token { get; }

    public string Server { get; }

    public string Channel { get; }

    public string ReleaseChannel { get; }
}
