using System.Linq;
using System.Threading;
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

    public async Task PublishAsync(Status message, CancellationToken cancellationToken)
    {
        if (message.State == "pending")
        {
            // don't output messages for pending builds
            return;
        }

        await this._bot.PublishAsync(BuildStatusMessage(message));
    }

    private static EmbedBuilder BuildStatusMessage(in Status message)
    {
        Branch lastBranch = message.Branches[^1];

        return new EmbedBuilder().WithTitle($"{message.Description} for {message.Context} from {message.Repository.Name} ({lastBranch.Name})")
                                 .WithUrl(message.TargetUrl)
                                 .WithDescription($"Built at {message.StatusCommit.Sha}")
                                 .WithColor(GetEmbedColor(message))
                                 .WithFields(GetFields(message));
    }

    private static EmbedFieldBuilder[] GetFields(in Status message)
    {
        StatusCommit statusCommit = message.StatusCommit;

        return new[]
               {
                   new EmbedFieldBuilder().WithName("Head commit")
                                          .WithValue($"{statusCommit.Author.Login} - {statusCommit.Commit.Message}"),
                   new EmbedFieldBuilder().WithName("Branch")
                                          .WithValue(message.Branches.Select(b => b.Name)
                                                            .FirstOrDefault())
               };
    }

    private static Color GetEmbedColor(in Status message)
    {
        if (message.State == "success")
        {
            return Color.Green;
        }

        if (message.State == "failure")
        {
            return Color.Red;
        }

        return Color.Default;
    }
}