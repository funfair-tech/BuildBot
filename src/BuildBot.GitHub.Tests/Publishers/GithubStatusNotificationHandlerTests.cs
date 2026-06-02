using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.GitHub.Models;
using BuildBot.GitHub.Publishers;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.GitHub.Tests.Publishers;

public sealed class GithubStatusNotificationHandlerTests : TestBase
{
    private static readonly DateTime COMMIT_TIMESTAMP = new(
        year: 2024,
        month: 1,
        day: 1,
        hour: 0,
        minute: 0,
        second: 0,
        kind: DateTimeKind.Utc
    );

    private static Status MakeStatus(string state)
    {
        Repository repository = new(
            id: 1,
            name: "BuildBot",
            fullName: "funfair-tech/BuildBot",
            owner: new Owner(avatarUrl: "https://example.com/avatar.png")
        );

        Commit commit = new(
            id: "abc123",
            sha: "abc123",
            treeId: "tree123",
            distinct: true,
            message: "Fix something",
            timeStamp: COMMIT_TIMESTAMP,
            url: "https://github.com/funfair-tech/BuildBot/commit/abc123",
            added: [],
            removed: [],
            modified: [],
            author: new CommitUser(name: "Test User", email: "test@example.com", username: "testuser"),
            committer: new CommitUser(name: "Test User", email: "test@example.com", username: "testuser")
        );

        StatusCommit statusCommit = new(commit: commit, sha: "abc123", author: new Author(login: "testuser"));

        return new Status(
            targetUrl: "https://ci.example.com/builds/123",
            repository: repository,
            context: "ci/build",
            state: state,
            branches: [new Branch(name: "main")],
            description: "Build finished",
            statusCommit: statusCommit
        );
    }

    private (IMediator mediator, GithubStatusNotificationHandler handler) CreateHandler()
    {
        IMediator mediator = GetSubstitute<IMediator>();
        ILogger<GithubStatusNotificationHandler> logger = this.GetTypedLogger<GithubStatusNotificationHandler>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return (mediator, new GithubStatusNotificationHandler(mediator: mediator, logger: logger));
    }

    [Fact]
    public async Task PendingStateShouldNotPublishAsync()
    {
        (IMediator mediator, GithubStatusNotificationHandler handler) = this.CreateHandler();

        Status status = MakeStatus(state: "pending");
        GithubStatus notification = new(status);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("error")]
    public async Task NonPendingStateShouldPublishAsync(string state)
    {
        (IMediator mediator, GithubStatusNotificationHandler handler) = this.CreateHandler();

        Status status = MakeStatus(state: state);
        GithubStatus notification = new(status);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }
}
