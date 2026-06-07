using System;
using System.Collections.Generic;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class CommitTests : TestBase
{
    private static readonly DateTime Timestamp = MockDateTimeSources.Past.GetUtcNow().UtcDateTime;

    private static readonly CommitUser CommitAuthor = new(
        name: "Test User",
        email: "test@example.com",
        username: "testuser"
    );

    private static readonly CommitUser CommitCommitter = new(
        name: "Committer User",
        email: "committer@example.com",
        username: "committeruser"
    );

    [Fact]
    public void IdPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: "abc123", actual: commit.Id);
    }

    [Fact]
    public void ShaPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: "sha456", actual: commit.Sha);
    }

    [Fact]
    public void TreeIdPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: "tree789", actual: commit.TreeId);
    }

    [Fact]
    public void DistinctPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.True(commit.Distinct, userMessage: "Distinct should match the constructor argument");
    }

    [Fact]
    public void MessagePropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: "Initial commit", actual: commit.Message);
    }

    [Fact]
    public void TimeStampPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: Timestamp, actual: commit.TimeStamp);
    }

    [Fact]
    public void UrlPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: "https://github.com/example/repo/commit/abc123", actual: commit.Url);
    }

    [Fact]
    public void AddedPropertyMatchesConstructorArgument()
    {
        IReadOnlyList<string> added = ["file1.txt", "file2.txt"];

        Commit commit = BuildDefaultCommit(added: added);

        Assert.Equal(expected: added, actual: commit.Added);
    }

    [Fact]
    public void RemovedPropertyMatchesConstructorArgument()
    {
        IReadOnlyList<string> removed = ["old.txt"];

        Commit commit = BuildDefaultCommit(removed: removed);

        Assert.Equal(expected: removed, actual: commit.Removed);
    }

    [Fact]
    public void ModifiedPropertyMatchesConstructorArgument()
    {
        IReadOnlyList<string> modified = ["changed.txt"];

        Commit commit = BuildDefaultCommit(modified: modified);

        Assert.Equal(expected: modified, actual: commit.Modified);
    }

    [Fact]
    public void AuthorPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: CommitAuthor, actual: commit.Author);
    }

    [Fact]
    public void CommitterPropertyMatchesConstructorArgument()
    {
        Commit commit = BuildDefaultCommit();

        Assert.Equal(expected: CommitCommitter, actual: commit.Committer);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Commit first = BuildDefaultCommit();
        Commit second = BuildDefaultCommit();

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentIdsAreNotEqual()
    {
        Commit first = BuildDefaultCommit();
        Commit second = BuildDefaultCommit(id: "def456");

        Assert.NotEqual(expected: first, actual: second);
    }

    private static Commit BuildDefaultCommit(
        string id = "abc123",
        string sha = "sha456",
        string treeId = "tree789",
        bool distinct = true,
        string message = "Initial commit",
        string url = "https://github.com/example/repo/commit/abc123",
        IReadOnlyList<string>? added = null,
        IReadOnlyList<string>? removed = null,
        IReadOnlyList<string>? modified = null
    )
    {
        return new Commit(
            id: id,
            sha: sha,
            treeId: treeId,
            distinct: distinct,
            message: message,
            timeStamp: Timestamp,
            url: url,
            added: added ?? [],
            removed: removed ?? [],
            modified: modified ?? [],
            author: CommitAuthor,
            committer: CommitCommitter
        );
    }
}
