using System;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace BuildBot.Discord.Services;

internal sealed class DiscordChannelAdapter : IDiscordChannel
{
    private readonly SocketTextChannel _channel;

    public DiscordChannelAdapter(SocketTextChannel channel)
    {
        this._channel = channel;
    }

    public string Name => this._channel.Name;

    public IDisposable EnterTypingState()
    {
        return this._channel.EnterTypingState();
    }

    public async Task<(string SentToChannel, string MessageContent)> SendMessageAsync(Embed embed)
    {
        RestUserMessage msg = await this._channel.SendMessageAsync(text: string.Empty, embed: embed);

        return (msg.Channel.Name, msg.CleanContent);
    }
}
