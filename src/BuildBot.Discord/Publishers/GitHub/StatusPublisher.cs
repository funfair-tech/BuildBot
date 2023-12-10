using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.ServiceModel.GitHub;
using Discord;

namespace BuildBot.Discord.Publishers.GitHub;

public sealed class StatusPublisher : IPublisher<Status>
{
    private static readonly IReadOnlyList<ColourMapping> ColourMappings =
    [
        new(State: "success", Colour: Color.Green),
        new(State: "failure", Colour: Color.Red)
    ];

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

    private static IReadOnlyList<EmbedFieldBuilder> GetFields(in Status message)
    {
        return
        [
            AddHeadCommitEmbed(message.StatusCommit),
            AddBranchEmbed(message)
        ];
    }

    private static EmbedFieldBuilder AddBranchEmbed(Status message)
    {
        return new EmbedFieldBuilder().WithName("Branch")
                                      .WithValue(message.Branches.Select(b => b.Name)
                                                        .FirstOrDefault());
    }

    private static EmbedFieldBuilder AddHeadCommitEmbed(StatusCommit statusCommit)
    {
        return new EmbedFieldBuilder().WithName("Head commit")
                                      .WithValue($"{statusCommit.Author.Login} - {statusCommit.Commit.Message}");
    }

    private static Color GetEmbedColor(Status message)
    {
        return ColourMappings.Where(mapping => mapping.IsMatch(message))
                             .Select(mapping => mapping.Colour)
                             .FirstOrDefault(Color.Default);
    }

    [DebuggerDisplay("{State} => {Colour}")]
    private readonly record struct ColourMapping(string State, Color Colour)
    {
        public bool IsMatch(in Status message)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x: this.State, y: message.State);
        }
    }
}