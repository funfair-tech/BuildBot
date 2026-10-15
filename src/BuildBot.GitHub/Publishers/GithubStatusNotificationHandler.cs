using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.GitHub.Models;
using BuildBot.GitHub.Publishers.LoggingExtensions;
using BuildBot.ServiceModel.GitHub;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers;

public sealed class GithubStatusNotificationHandler : INotificationHandler<GithubStatus>
{
    private static readonly IReadOnlyList<ColourMapping> ColourMappings =
    [
        new(State: "success", Colour: DiscordColour.Green),
        new(State: "failure", Colour: DiscordColour.Red),
    ];

    private readonly ILogger<GithubStatusNotificationHandler> _logger;
    private readonly IMediator _mediator;

    public GithubStatusNotificationHandler(IMediator mediator, ILogger<GithubStatusNotificationHandler> logger)
    {
        this._mediator = mediator;
        this._logger = logger;
    }

    public ValueTask Handle(GithubStatus notification, CancellationToken cancellationToken)
    {
        this._logger.GitHubRef(notification.Model.Context);

        return this.PublishAsync(message: notification.Model, cancellationToken: cancellationToken);
    }

    private ValueTask PublishAsync(in Status message, in CancellationToken cancellationToken)
    {
        // don't output messages for pending builds
        return IsPendingBuild(message)
            ? ValueTask.CompletedTask
            : this._mediator.Publish(new BotMessage(BuildStatusMessage(message)), cancellationToken: cancellationToken);
    }

    private static bool IsPendingBuild(in Status message)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(x: message.State, y: "pending");
    }

    private static DiscordEmbed BuildStatusMessage(in Status message)
    {
        Branch? lastBranch = message.Branches.Count > 0 ? message.Branches[^1] : null;
        string branchName = lastBranch?.Name ?? "(unknown)";

        return new(
            Title: $"{message.Description} for {message.Context} from {message.Repository.Name} ({branchName})",
            Url: EmbedUrl.FromString(message.TargetUrl),
            Description: $"Built at {message.StatusCommit.Sha}",
            Colour: GetEmbedColour(message),
            Fields: GetFields(message)
        );
    }

    private static IReadOnlyList<DiscordEmbedField> GetFields(in Status message)
    {
        return [AddHeadCommitEmbed(message.StatusCommit), AddBranchEmbed(message)];
    }

    private static DiscordEmbedField AddBranchEmbed(in Status message)
    {
        return new(Name: "Branch", Value: message.Branches.Select(static b => b.Name).FirstOrDefault() ?? "(unknown)");
    }

    private static DiscordEmbedField AddHeadCommitEmbed(in StatusCommit statusCommit)
    {
        return new(Name: "Head commit", Value: $"{statusCommit.Author.Login} - {statusCommit.Commit.Message}");
    }

    private static DiscordColour? GetEmbedColour(Status message)
    {
        return ColourMappings
            .Where(mapping => mapping.IsMatch(message))
            .Select(static DiscordColour? (mapping) => mapping.Colour)
            .FirstOrDefault();
    }

    [DebuggerDisplay("{State} => {Colour}")]
    private readonly record struct ColourMapping(string State, DiscordColour Colour)
    {
        public bool IsMatch(in Status message)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x: this.State, y: message.State);
        }
    }
}
