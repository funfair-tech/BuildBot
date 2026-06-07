using System;
using System.Collections.Generic;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class StatusTests : TestBase
{
    private static readonly DateTime Timestamp = MockDateTimeSources.Past.GetUtcNow().UtcDateTime;

    private static readonly Repository Repository = new(
        id: 42,
        name: "BuildBot",
        fullName: "funfair-tech/BuildBot",
        owner: new Owner(avatarUrl: "https://example.com/avatar.png")
    );

    private static readonly StatusCommit StatusCommit = new(
        commit: new Commit(
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
        ),
        sha: "sha456",
        author: new Author(login: "octocat")
    );

    [Fact]
    public void TargetUrlPropertyMatchesConstructorArgument()
    {
        Status status = BuildDefaultStatus();

        Assert.Equal(expected: "https://ci.example.com/build/1", actual: status.TargetUrl);
    }

    [Fact]
    public void RepositoryPropertyMatchesConstructorArgument()
    {
        Status status = BuildDefaultStatus();

        Assert.Equal(expected: Repository, actual: status.Repository);
    }

    [Fact]
    public void ContextPropertyMatchesConstructorArgument()
    {
        Status status = BuildDefaultStatus();

        Assert.Equal(expected: "ci/build", actual: status.Context);
    }

    [Fact]
    public void StatePropertyMatchesConstructorArgument()
    {
        Status status = BuildDefaultStatus();

        Assert.Equal(expected: "success", actual: status.State);
    }

    [Fact]
    public void BranchesPropertyMatchesConstructorArgument()
    {
        IReadOnlyList<Branch> branches = [new Branch(name: "main")];

        Status status = BuildDefaultStatus(branches: branches);

        Assert.Equal(expected: branches, actual: status.Branches);
    }

    [Fact]
    public void DescriptionPropertyMatchesConstructorArgument()
    {
        Status status = BuildDefaultStatus();

        Assert.Equal(expected: "Build passed", actual: status.Description);
    }

    [Fact]
    public void StatusCommitPropertyMatchesConstructorArgument()
    {
        Status status = BuildDefaultStatus();

        Assert.Equal(expected: StatusCommit, actual: status.StatusCommit);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Status first = BuildDefaultStatus();
        Status second = BuildDefaultStatus();

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentStatesAreNotEqual()
    {
        Status first = BuildDefaultStatus();
        Status second = BuildDefaultStatus(state: "failure", description: "Build failed");

        Assert.NotEqual(expected: first, actual: second);
    }

    private static Status BuildDefaultStatus(
        string targetUrl = "https://ci.example.com/build/1",
        string context = "ci/build",
        string state = "success",
        IReadOnlyList<Branch>? branches = null,
        string description = "Build passed"
    )
    {
        return new Status(
            targetUrl: targetUrl,
            repository: Repository,
            context: context,
            state: state,
            branches: branches ?? [],
            description: description,
            statusCommit: StatusCommit
        );
    }
}
