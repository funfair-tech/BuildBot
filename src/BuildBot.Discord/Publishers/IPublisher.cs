using System.Threading.Tasks;

namespace BuildBot.Discord.Publishers
{
    public interface IPublisher<in T>
    {
        Task PublishAsync(T message);
    }
}