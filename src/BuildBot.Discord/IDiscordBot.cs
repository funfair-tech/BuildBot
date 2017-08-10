using Discord;
using System.Threading.Tasks;

namespace BuildBot.Discord
{
    public interface IDiscordBot
    {
        Task Publish(string message);
        Task Publish(EmbedBuilder builder);
    }
}
