using System;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class StatusCommitTests : TestBase
{
    private static readonly DateTime Timestamp = MockDateTimeSources.Past.GetUtcNow().UtcDateTime;

    private static readonly Author Author = new(login: "octocat");

    private static readonly Commit Commit = new(
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
    public void CommitPropertyMatchesConstructorArgument()
    {
        StatusCommit statusCommit = new(commit: Commit, sha: "sha456", author: Author);

        Assert.Equal(expected: Commit, actual: statusCommit.Commit);
    }

    [Fact]
    public void ShaPropertyMatchesConstructorArgument()
    {
        StatusCommit statusCommit = new(commit: Commit, sha: "sha456", author: Author);

        Assert.Equal(expected: "sha456", actual: statusCommit.Sha);
    }

    [Fact]
    public void AuthorPropertyMatchesConstructorArgument()
    {
        StatusCommit statusCommit = new(commit: Commit, sha: "sha456", author: Author);

        Assert.Equal(expected: Author, actual: statusCommit.Author);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        StatusCommit first = new(commit: Commit, sha: "sha456", author: Author);
        StatusCommit second = new(commit: Commit, sha: "sha456", author: Author);

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentShasAreNotEqual()
    {
        StatusCommit first = new(commit: Commit, sha: "sha456", author: Author);
        StatusCommit second = new(commit: Commit, sha: "xyz999", author: Author);

        Assert.NotEqual(expected: first, actual: second);
    }
}
