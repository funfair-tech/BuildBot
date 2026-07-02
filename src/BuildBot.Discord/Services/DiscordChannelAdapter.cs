using System;
using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord.Services;

public sealed class DiscordChannelAdapter : IDiscordChannel
{
    private readonly ITextChannel _channel;

    public DiscordChannelAdapter(ITextChannel channel)
    {
        this._channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public string Name => this._channel.Name;

    public IDisposable EnterTypingState()
    {
        return this._channel.EnterTypingState();
    }

    public async Task<(string SentToChannel, string MessageContent)> SendMessageAsync(Embed embed)
    {
        IUserMessage msg = await this._channel.SendMessageAsync(text: string.Empty, embed: embed);

        return (msg.Channel.Name, msg.CleanContent);
    }
}
