using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord;

public interface IDiscordBot
{
    ValueTask PublishAsync(EmbedBuilder builder, CancellationToken cancellationToken);

    ValueTask PublishToReleaseChannelAsync(EmbedBuilder builder, CancellationToken cancellationToken);
}