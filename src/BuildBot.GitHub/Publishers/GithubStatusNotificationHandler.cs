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
    private static readonly IReadOnlyList<ColorMapping> ColorMappings =
    [
        new(State: "success", Color: DiscordColor.Green),
        new(State: "failure", Color: DiscordColor.Red),
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
        string branchName = GetBranchName(message.Branches);

        return new(
            Title: $"{message.Description} for {message.Context} from {message.Repository.Name} ({branchName})",
            Url: EmbedUrl.FromString(message.TargetUrl),
            Description: $"Built at {message.StatusCommit.Sha}",
            Color: GetEmbedColor(message),
            Fields: [AddHeadCommitEmbed(message.StatusCommit), AddBranchEmbed(branchName)]
        );
    }

    private static string GetBranchName(IReadOnlyList<Branch> branches)
    {
        IReadOnlyList<string> names = [.. branches.Select(static b => b.Name)];

        return names.FirstOrDefault(IsReleaseBranch)
            ?? names.FirstOrDefault(IsMainBranch)
            ?? names.FirstOrDefault()
            ?? "(unknown)";
    }

    private static bool IsReleaseBranch(string name)
    {
        return name.StartsWith(value: "release/", comparisonType: StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMainBranch(string name)
    {
        return StringComparer.Ordinal.Equals(x: name, y: "main");
    }

    private static DiscordEmbedField AddBranchEmbed(string branchName)
    {
        return new(Name: "Branch", Value: branchName);
    }

    private static DiscordEmbedField AddHeadCommitEmbed(in StatusCommit statusCommit)
    {
        return new(Name: "Head commit", Value: $"{statusCommit.Author.Login} - {statusCommit.Commit.Message}");
    }

    private static DiscordColor? GetEmbedColor(Status message)
    {
        return ColorMappings
            .Where(mapping => mapping.IsMatch(message))
            .Select(static DiscordColor? (mapping) => mapping.Color)
            .FirstOrDefault();
    }

    [DebuggerDisplay("{State} => {Color}")]
    private readonly record struct ColorMapping(string State, DiscordColor Color)
    {
        public bool IsMatch(in Status message)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x: this.State, y: message.State);
        }
    }
}
