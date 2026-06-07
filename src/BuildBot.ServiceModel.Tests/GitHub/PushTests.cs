using System;
using System.Collections.Generic;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class PushTests : TestBase
{
    private static readonly DateTime Timestamp = MockDateTimeSources.Past.GetUtcNow().UtcDateTime;

    private static readonly Repository Repository = new(
        id: 42,
        name: "BuildBot",
        fullName: "funfair-tech/BuildBot",
        owner: new Owner(avatarUrl: "https://example.com/avatar.png")
    );

    private static readonly Pusher Pusher = new(name: "deployer", email: "deploy@example.com");

    private static readonly Commit HeadCommit = new(
        id: "abc123",
        sha: "sha456",
        treeId: "tree789",
        distinct: true,
        message: "Fix bug",
        timeStamp: Timestamp,
        url: "https://github.com/example/repo/commit/abc123",
        added: [],
        removed: [],
        modified: [],
        author: new CommitUser(name: "Test User", email: "test@example.com", username: "testuser"),
        committer: new CommitUser(name: "Test User", email: "test@example.com", username: "testuser")
    );

    [Fact]
    public void RefPropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: "refs/heads/main", actual: push.Ref);
    }

    [Fact]
    public void BeforePropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: "before-sha", actual: push.Before);
    }

    [Fact]
    public void AfterPropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: "after-sha", actual: push.After);
    }

    [Fact]
    public void HeadCommitPropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: HeadCommit, actual: push.HeadCommit);
    }

    [Fact]
    public void CommitsPropertyMatchesConstructorArgument()
    {
        IReadOnlyList<Commit> commits = [HeadCommit];

        Push push = BuildDefaultPush(commits: commits);

        Assert.Equal(expected: commits, actual: push.Commits);
    }

    [Fact]
    public void RepositoryPropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: Repository, actual: push.Repository);
    }

    [Fact]
    public void PusherPropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: Pusher, actual: push.Pusher);
    }

    [Fact]
    public void CompareUrlPropertyMatchesConstructorArgument()
    {
        Push push = BuildDefaultPush();

        Assert.Equal(expected: "https://github.com/example/repo/compare/before...after", actual: push.CompareUrl);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Push first = BuildDefaultPush();
        Push second = BuildDefaultPush();

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentRefsAreNotEqual()
    {
        Push first = BuildDefaultPush();
        Push second = BuildDefaultPush(@ref: "refs/heads/develop");

        Assert.NotEqual(expected: first, actual: second);
    }

    private static Push BuildDefaultPush(
        string @ref = "refs/heads/main",
        string before = "before-sha",
        string after = "after-sha",
        IReadOnlyList<Commit>? commits = null,
        string compareUrl = "https://github.com/example/repo/compare/before...after"
    )
    {
        return new Push(
            @ref: @ref,
            before: before,
            after: after,
            headCommit: HeadCommit,
            commits: commits ?? [],
            repository: Repository,
            pusher: Pusher,
            compareUrl: compareUrl
        );
    }
}
