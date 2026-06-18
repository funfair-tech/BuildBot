using System;
using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord.Services;

public interface IDiscordChannel
{
    string Name { get; }

    IDisposable EnterTypingState();

    Task<(string SentToChannel, string MessageContent)> SendMessageAsync(Embed embed);
}
