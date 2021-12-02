using System.Linq;
using System.Threading.Tasks;
using BuildBot.ServiceModel.GitHub;
using Discord;

namespace BuildBot.Discord.Publishers.GitHub;

public sealed class StatusPublisher : IPublisher<Status>
{
    private readonly IDiscordBot _bot;

    public StatusPublisher(IDiscordBot bot)
    {
        this._bot = bot;
    }

    public async Task PublishAsync(Status message)
    {
        if (message.State == "pending")
        {
            // don't output messages for pending builds
            return;
        }

        EmbedBuilder builder = new();
        builder.WithTitle($"{message.Description} for {message.Context} from {message.Repository.Name} ({message.Branches.Last().Name})");
        builder.WithUrl(message.TargetUrl);
        builder.Description = $"Built at {message.StatusCommit.Sha}";

        if (message.State == "success")
        {
            builder.Color = Color.Green;
        }

        if (message.State == "failure")
        {
            builder.Color = Color.Red;
        }

        StatusCommit statusCommit = message.StatusCommit;
        EmbedFieldBuilder commitFieldBuilder = new() { Name = "Head commit", Value = $"{statusCommit.Author.Login} - {statusCommit.Commit.Message}" };
        builder.AddField(commitFieldBuilder);

        await this._bot.PublishAsync(builder);
    }
}