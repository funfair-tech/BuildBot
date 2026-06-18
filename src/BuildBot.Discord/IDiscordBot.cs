using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord;

public interface IDiscordBot
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    ValueTask PublishAsync(EmbedBuilder builder, CancellationToken cancellationToken);

    ValueTask PublishToReleaseChannelAsync(EmbedBuilder builder, CancellationToken cancellationToken);
}
