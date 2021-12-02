using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord;

public interface IDiscordBot
{
    Task PublishAsync(EmbedBuilder builder);

    Task PublishToReleaseChannelAsync(EmbedBuilder builder);
}