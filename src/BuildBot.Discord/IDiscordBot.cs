using System.Threading.Tasks;
using Discord;

namespace BuildBot.Discord
{
    public interface IDiscordBot
    {
        Task PublishAsync(string message);

        Task PublishAsync(EmbedBuilder builder);
    }
}