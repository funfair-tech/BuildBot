using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.ServiceModel.GitHub;
using Discord;

namespace BuildBot.Discord.Publishers.GitHub;

public sealed class PushPublisher : IPublisher<Push>
{
    private static readonly IReadOnlyList<string> MainBranches =
    [
        "main",
        "master"
    ];

    private readonly IDiscordBot _bot;

    public PushPublisher(IDiscordBot bot)
    {
        this._bot = bot;
    }

    public async Task PublishAsync(Push message, CancellationToken cancellationToken)
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

        await this._bot.PublishAsync(builder);
    }

    private static bool IsIgnoredRepo(in Push message)
    {
        return message.Repository.Name == "TeamCity";
    }

    private static bool IsIgnoredCommit(in Push message)
    {
        if (message.Commits.Any(predicate: c => c.Message.StartsWith(value: "chore", comparisonType: StringComparison.Ordinal)))
        {
            return true;
        }

        string branch = BranchName(message);

        if (IsRepoMainBranch(branch))
        {
            static bool IsPackageUpdate(Commit c)
            {
                return c.Message.StartsWith(value: "FF-1429", comparisonType: StringComparison.Ordinal) || c.Message.StartsWith(value: "[FF-1429]", comparisonType: StringComparison.Ordinal);
            }

            if (message.Commits.Any(predicate: IsPackageUpdate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRepoMainBranch(string branch)
    {
        return MainBranches.Contains(value: branch, comparer: StringComparer.Ordinal);
    }

    private static EmbedBuilder BuildPushEmbed(in Push message)
    {
        string commitString = GetCommitString(message);
        string title = GetCommitTitle(message: message, commitString: commitString);

        EmbedBuilder builder = new EmbedBuilder().WithTitle(title)
                                                 .WithUrl(message.CompareUrl);

        return message.Commits.Aggregate(builder, AddField);
    }

    private static EmbedBuilder AddField(EmbedBuilder current, Commit commit)
    {
        EmbedFieldBuilder efb = new()
                                {
                                    Name = $"**{commit.Author.Username ?? commit.Author.Name}** - {commit.Message}",
                                    Value = $"{commit.Added.Count} added, {commit.Modified.Count} modified, {commit.Removed.Count} removed"
                                };

        return current.AddField(efb);
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