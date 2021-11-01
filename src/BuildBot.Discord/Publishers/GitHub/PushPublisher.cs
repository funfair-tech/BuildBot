using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildBot.ServiceModel.GitHub;
using Discord;

namespace BuildBot.Discord.Publishers.GitHub
{
    /// <summary>
    ///     Publish the GitHub "push" event to Discord
    /// </summary>
    public sealed class PushPublisher : IPublisher<Push>
    {
        private readonly IDiscordBot _bot;

        public PushPublisher(IDiscordBot bot)
        {
            this._bot = bot;
        }

        /// <inheritdoc />
        public async Task PublishAsync(Push message)
        {
            // only publish Push messages if there are commits, otherwise we'll be publishing
            // all the tagging that goes on.
            if (message.Commits.Count == 0)
            {
                return;
            }

            if (message.Repository.Name == @"TeamCity")
            {
                // ignore commits to TeamCity, it's pretty annoying!
                return;
            }

            if (message.Commits.Count == 1 && message.Commits.Any(predicate: c => c.Message.StartsWith(value: "chore", comparisonType: StringComparison.Ordinal)))
            {
                // ignore commits which contain "chore"
                return;
            }

            string commitString = message.Commits.Count > 1 ? $"{message.Commits.Count} commits" : $"{message.Commits.Count} commit";
            string title = $"{message.Pusher.Name} pushed {commitString} to {message.Repository.Name} ({message.Ref.Substring("refs/heads/".Length)})";

            EmbedBuilder builder = new();
            builder.WithTitle(title);
            builder.WithUrl(message.CompareUrl);

            foreach (Commit commit in message.Commits)
            {
                EmbedFieldBuilder commitFieldBuilder = new()
                                                       {
                                                           Name = $"**{commit.Author.Username ?? commit.Author.Name}** - {commit.Message}",
                                                           Value = $"{commit.Added.Count} added, {commit.Modified.Count} modified, {commit.Removed.Count} removed"
                                                       };

                StringBuilder commitBuilder = new();

                if (commit.Added.Count != 0)
                {
                    commitBuilder.Append(commit.Added.Count)
                                 .AppendLine(" added");
                }

                if (commit.Modified.Count != 0)
                {
                    commitBuilder.Append(commit.Modified.Count)
                                 .AppendLine(" modified");
                }

                if (commit.Removed.Count != 0)
                {
                    commitBuilder.Append(commit.Removed.Count)
                                 .AppendLine(" removed");
                }

                builder.AddField(commitFieldBuilder);
            }

            await this._bot.PublishAsync(builder);
        }
    }
}