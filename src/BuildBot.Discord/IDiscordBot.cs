using System.Threading.Tasks;

namespace BuildBot.Discord
{
    public interface IDiscordBot
    {
        Task PublishAsync(EmbedBuilder builder);

        Task PublishToReleaseChannelAsync(EmbedBuilder builder);
    }
}