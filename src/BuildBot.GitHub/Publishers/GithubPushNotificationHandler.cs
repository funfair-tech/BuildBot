using System;
using System.Collections.Generic;
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

public sealed class GithubPushNotificationHandler : INotificationHandler<GithubPush>
{
    private static readonly IReadOnlyList<string> MainBranches =
    [
        "main",
        "master"
    ];

    private static readonly IReadOnlyList<string> PackageUpdatePrefixes =
    [
        "FF-1429",
        "[FF-1429]",
        "Dependencies",
        "[Dependencies]"
    ];

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

        EmbedBuilder builder = BuildPushEmbed(message);

        await this._mediator.Publish(new BotMessage(builder), cancellationToken: cancellationToken);
    }

    private static bool IsIgnoredRepo(in Push message)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(x: message.Repository.Name, y: "TeamCity");
    }

    private static bool IsIgnoredCommit(in Push message)
    {
        if (message.Commits.Any(predicate: c => c.Message.StartsWith(value: "chore", comparisonType: StringComparison.Ordinal)))
        {
            return true;
        }

        string branch = BranchName(message);

        return IsRepoMainBranch(branch) && message.Commits.Any(predicate: IsPackageUpdate);
    }

    private static bool IsPackageUpdate(Commit c)
    {
        return PackageUpdatePrefixes.Any(prefix => c.Message.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal));
    }

    private static bool IsRepoMainBranch(string branch)
    {
        return MainBranches.Contains(value: branch, comparer: StringComparer.Ordinal);
    }

    private static EmbedBuilder BuildPushEmbed(in Push message)
    {
        return message.Commits.Aggregate(CreateBasicEmbed(message), func: AddCommit);
    }

    private static EmbedBuilder CreateBasicEmbed(Push message)
    {
        string commitString = GetCommitString(message);
        string title = GetCommitTitle(message: message, commitString: commitString);

        return new EmbedBuilder().WithTitle(title)
                                 .WithUrl(message.CompareUrl);
    }

    private static EmbedBuilder AddCommit(EmbedBuilder current, Commit commit)
    {
        return current.AddField(CreateCommitEmbed(commit));
    }

    private static EmbedFieldBuilder CreateCommitEmbed(Commit commit)
    {
        return new EmbedFieldBuilder().WithName($"**{commit.Author.Username ?? commit.Author.Name}** - {commit.Message}")
                                      .WithValue($"{commit.Added.Count} added, {commit.Modified.Count} modified, {commit.Removed.Count} removed");
    }

    private static string GetCommitTitle(in Push message, string commitString)
    {
        return $"{message.Pusher.Name} pushed {commitString} to {message.Repository.Name} ({BranchName(message)})";
    }

    private static string BranchName(in Push message)
    {
        return message.Ref.Substring("refs/heads/".Length);
    }

    private static string GetCommitString(in Push message)
    {
        return message.Commits.Count > 1
            ? $"{message.Commits.Count} commits"
            : $"{message.Commits.Count} commit";
    }
}