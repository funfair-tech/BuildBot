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

        StatusCommit statusCommit = message.StatusCommit;
        EmbedBuilder builder = new EmbedBuilder().WithTitle($"{message.Description} for {message.Context} from {message.Repository.Name} ({message.Branches.Last().Name})")
                                                 .WithUrl(message.TargetUrl)
                                                 .WithDescription($"Built at {message.StatusCommit.Sha}")
                                                 .WithColor(GetEmbedColor(message))
                                                 .WithFields(new EmbedFieldBuilder().WithName("Head commit")
                                                                                    .WithValue($"{statusCommit.Author.Login} - {statusCommit.Commit.Message}"),
                                                             new EmbedFieldBuilder().WithName("Branch")
                                                                                    .WithValue(message.Branches.FirstOrDefault()));

        await this._bot.PublishAsync(builder);
    }

    private static Color GetEmbedColor(Status message)
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