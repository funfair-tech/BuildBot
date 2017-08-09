using System.Threading.Tasks;

namespace BuildBot.Discord.Publishers
{
    public interface IPublisher<T>
    {
        Task Publish(T message);
    }
}
