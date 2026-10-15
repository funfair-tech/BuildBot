using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
using BuildBot.GitHub.Helpers;
using BuildBot.GitHub.Models;
using BuildBot.GitHub.Publishers.LoggingExtensions;
using BuildBot.ServiceModel.GitHub;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.GitHub.Publishers;

public sealed class GithubPushNotificationHandler : INotificationHandler<GithubPush>
{
    private readonly ILogger<GithubPushNotificationHandler> _logger;
    private readonly IMediator _mediator;

    public GithubPushNotificationHandler(IMediator mediator, ILogger<GithubPushNotificationHandler> logger)
    {
        this._mediator = mediator;
        this._logger = logger;
    }

    public ValueTask Handle(GithubPush notification, CancellationToken cancellationToken)
    {
        this._logger.GitHubRef(notification.Model.Ref);

        return this.PublishAsync(message: notification.Model, cancellationToken: cancellationToken);
    }

    private async ValueTask PublishAsync(Push message, CancellationToken cancellationToken)
    {
        // only publish Push messages if there are commits, otherwise we'll be publishing
        // all the tagging that goes on.
        if (message.Commits.Count == 0)
        {
            return;
        }

        if (IsIgnoredRepo(message))
        {
            // ignore commits to TeamCity, it's pretty annoying!
            return;
        }

        if (message.Commits.Count == 1 && IsIgnoredCommit(message))
        {
            // ignore commits which contain "chore"
            return;
        }

        DiscordEmbed embed = BuildPushEmbed(message);

        await this._mediator.Publish(new BotMessage(embed), cancellationToken: cancellationToken);
    }

    private static bool IsIgnoredRepo(in Push message)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(x: message.Repository.Name, y: "TeamCity");
    }

    private static bool IsIgnoredCommit(in Push message)
    {
        if (
            message.Commits.Any(predicate: static c =>
                c.Message.StartsWith(value: "chore", comparisonType: StringComparison.Ordinal)
            )
        )
        {
            return true;
        }

        return MainBranchDetector.IsRepoMainBranch(BranchName(message))
            && message.Commits.Any(predicate: IsPackageUpdate);

        static bool IsPackageUpdate(Commit commit)
        {
            return PackageUpdateDetector.IsPackageUpdate(commit);
        }
    }

    private static DiscordEmbed BuildPushEmbed(in Push message)
    {
        string commitString = GetCommitString(message);
        string title = GetCommitTitle(message: message, commitString: commitString);

        return new(
            Title: title,
            Url: EmbedUrl.FromString(message.CompareUrl),
            Description: null,
            Color: null,
            Fields: CreateCommitFields(message.Commits)
        );
    }

    private static IReadOnlyList<DiscordEmbedField> CreateCommitFields(IReadOnlyList<Commit> commits)
    {
        return [.. commits.Select(CreateCommitEmbed)];
    }

    private static DiscordEmbedField CreateCommitEmbed(Commit commit)
    {
        return new(
            Name: $"**{commit.Author.Username ?? commit.Author.Name}** - {commit.Message}",
            Value: $"{commit.Added.Count} added, {commit.Modified.Count} modified, {commit.Removed.Count} removed"
        );
    }

    private static string GetCommitTitle(in Push message, string commitString)
    {
        return $"{message.Pusher.Name} pushed {commitString} to {message.Repository.Name} ({BranchName(message)})";
    }

    private static string BranchName(in Push message)
    {
        const string refsHeadsPrefix = "refs/heads/";

        return message.Ref[refsHeadsPrefix.Length..];
    }

    private static string GetCommitString(in Push message)
    {
        return message.Commits.Count > 1 ? $"{message.Commits.Count} commits" : $"{message.Commits.Count} commit";
    }
}
