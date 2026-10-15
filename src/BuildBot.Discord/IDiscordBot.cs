using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;

namespace BuildBot.Discord;

public interface IDiscordBot
{
    ValueTask PublishAsync(DiscordEmbed embed, CancellationToken cancellationToken);
}
