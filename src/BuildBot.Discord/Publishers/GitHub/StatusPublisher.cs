using BuildBot.ServiceModel.GitHub;
using Discord;
using System.Threading.Tasks;

namespace BuildBot.Discord.Publishers.GitHub
{
    public class StatusPublisher : IPublisher<Status>
    {
        private readonly IDiscordBot _bot;

        public StatusPublisher(IDiscordBot bot)
        {
            this._bot = bot;
        }

        public async Task Publish(Status status)
        {
            if (status.State == "pending")
            {
                // don't output messages for pending builds
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{status.Description} for {status.Repository.Name} ({status.Branches[0].Name})");
            builder.WithUrl(status.TargetUrl);
            builder.Description = $"Built at {status.StatusCommit.Sha}";

            if (status.State == "success")
            {
                builder.Color = Color.Green;
            }

            if (status.State == "failure")
            {
                builder.Color = Color.Red;
            }

            EmbedFieldBuilder commitFieldBuilder = new EmbedFieldBuilder();
            commitFieldBuilder.Name = "Head commit";
            commitFieldBuilder.Value = $"{status.StatusCommit.Author.Login} - {status.StatusCommit.Commit.Message}";
            builder.AddField(commitFieldBuilder);

            await this._bot.Publish(builder);
        }
    }
}
