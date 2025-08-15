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
using Discord;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers;

public sealed class GithubStatusNotificationHandler : INotificationHandler<GithubStatus>
{
    private static readonly IReadOnlyList<ColourMapping> ColourMappings =
    [
        new(State: "success", Colour: Color.Green),
        new(State: "failure", Colour: Color.Red),
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

    private static EmbedBuilder BuildStatusMessage(in Status message)
    {
        Branch lastBranch = message.Branches[^1];

        return new EmbedBuilder()
            .WithTitle(
                $"{message.Description} for {message.Context} from {message.Repository.Name} ({lastBranch.Name})"
            )
            .WithUrl(message.TargetUrl)
            .WithDescription($"Built at {message.StatusCommit.Sha}")
            .WithColor(GetEmbedColor(message))
            .WithFields(GetFields(message));
    }

    private static IReadOnlyList<EmbedFieldBuilder> GetFields(in Status message)
    {
        return [AddHeadCommitEmbed(message.StatusCommit), AddBranchEmbed(message)];
    }

    private static EmbedFieldBuilder AddBranchEmbed(in Status message)
    {
        return new EmbedFieldBuilder()
            .WithName("Branch")
            .WithValue(message.Branches.Select(static b => b.Name).FirstOrDefault());
    }

    private static EmbedFieldBuilder AddHeadCommitEmbed(in StatusCommit statusCommit)
    {
        return new EmbedFieldBuilder()
            .WithName("Head commit")
            .WithValue($"{statusCommit.Author.Login} - {statusCommit.Commit.Message}");
    }

    private static Color GetEmbedColor(Status message)
    {
        return ColourMappings
            .Where(mapping => mapping.IsMatch(message))
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
