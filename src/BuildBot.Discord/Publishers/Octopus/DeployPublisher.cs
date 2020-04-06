using System.Threading.Tasks;
using BuildBot.ServiceModel.Octopus;
using Discord;

namespace BuildBot.Discord.Publishers.Octopus
{
    public class DeployPublisher : IPublisher<Deploy>
    {
        private readonly IDiscordBot _bot;

        public DeployPublisher(IDiscordBot bot)
        {
            this._bot = bot;
        }

        public Task PublishAsync(Deploy message)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(message.Payload.Event.Message);
            builder.WithUrl(message.Payload.ServerAuditUri);

            if (message.Payload.Event.Category == "DeploymentSucceeded")
            {
                builder.Color = Color.Green;
            }

            if (message.Payload.Event.Category == "DeploymentFailed")
            {
                builder.Color = Color.Red;
            }

            return this._bot.PublishAsync(builder);
        }
    }
}