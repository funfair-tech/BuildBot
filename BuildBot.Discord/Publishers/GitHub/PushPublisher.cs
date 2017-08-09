using BuildBot.ServiceModel.GitHub;
using Discord;
using System.Collections.Generic;
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

        private void FormatChanges(List<string> changes, string type, StringBuilder builder)
        {
            if (changes.Any())
            {
                builder.AppendLine();
                builder.AppendLine($"{type}:");
                builder.Append("```");

                for (int i = 0; i < changes.Count; i++)
                {
                    builder.AppendLine(changes[i]);
                    if (i == 6)
                    {
                        builder.AppendLine("**truncated**");
                        break;
                    }
                }

                builder.Append("```");
            }
        }

        public async Task Publish(Push push)
        {
            // only publish Push messages if there are commits, otherwise we'll be publishing
            // all the tagging that goes on.
            if (!push.Commits.Any())
            {
                return;
            }

            string commitString = push.Commits.Count() > 1 ? $"{push.Commits.Count()} commits" : $"{push.Commits.Count()} commit";
            string title = $"Pushed {commitString} to {push.Repository.Name} ({push.Ref.Substring("refs/heads/".Length)})";

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(title);
            builder.WithAuthor(push.Pusher.Name, $"https://github.com/{push.Pusher.Name}.png");
            
            foreach (Commit commit in push.Commits)
            {
                EmbedFieldBuilder commitFieldBuilder = new EmbedFieldBuilder();
                commitFieldBuilder.Name = $"{commit.Url}";

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"**{commit.Author.Username}** | **{commit.Message}**");

                this.FormatChanges(commit.Added, "Added", stringBuilder);
                this.FormatChanges(commit.Modified, "Modified", stringBuilder);
                this.FormatChanges(commit.Removed, "Removed", stringBuilder);

                commitFieldBuilder.Value = stringBuilder.ToString();
                builder.AddField(commitFieldBuilder);
            }

            await this._bot.Publish(builder.Build());
        }
    }
}
