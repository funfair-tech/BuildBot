using BuildBot.ServiceModel.GitHub;
using Discord;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBot.Discord.Publishers.GitHub
{
    /// <summary>
    /// Publish the GitHub "push" event to Discord
    /// </summary>
    public class PushPublisher : IPublisher<Push>
    {
        private readonly IDiscordBot _bot;

        public PushPublisher(IDiscordBot bot)
        {
            this._bot = bot;
        }

        public async Task Publish(Push push)
        {
            // only publish Push messages if there are commits, otherwise we'll be publishing
            // all the tagging that goes on.
            if (!push.Commits.Any())
            {
                return;
            }

            if (push.Repository.Name == "TeamCity")
            {
                // ignore commits to TeamCity, it's pretty annoying!
                return;
            }

            string commitString = push.Commits.Count() > 1 ? $"{push.Commits.Count()} commits" : $"{push.Commits.Count()} commit";
            string title = $"{push.Pusher.Name} pushed {commitString} to {push.Repository.Name} ({push.Ref.Substring("refs/heads/".Length)})";

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(title);
            builder.WithUrl(push.CompareUrl);

            foreach (Commit commit in push.Commits)
            {
                EmbedFieldBuilder commitFieldBuilder = new EmbedFieldBuilder();
                commitFieldBuilder.Name = $"**{commit.Author.Username ?? commit.Author.Name}** - {commit.Message}";
                commitFieldBuilder.Value = $"{commit.Added.Count()} added, {commit.Modified.Count()} modified, {commit.Removed.Count()} removed";

                StringBuilder commitBuilder = new StringBuilder();
                if (commit.Added.Count() > 0)
                {
                    commitBuilder.AppendLine($"{commit.Added.Count()} added");
                }

                if (commit.Modified.Count() > 0)
                {
                    commitBuilder.AppendLine($"{commit.Modified.Count()} modified");
                }

                if (commit.Removed.Count() > 0)
                {
                    commitBuilder.AppendLine($"{commit.Removed.Count()} removed");
                }

                //commitFieldBuilder.Value = commitBuilder.ToString();
                builder.AddField(commitFieldBuilder);
            }

            await this._bot.Publish(builder);
        }
    }
}
