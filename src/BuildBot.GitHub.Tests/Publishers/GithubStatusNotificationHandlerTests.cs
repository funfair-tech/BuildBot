using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Models;
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
    private static readonly DateTime COMMIT_TIMESTAMP = FunFair
        .Test.Infrastructure.Mocks.MockDateTimeSources.Past.GetUtcNow()
        .UtcDateTime;

    private static Status MakeStatus(string state)
    {
        return MakeStatusWithBranches(state: state, branches: [new Branch(name: "main")]);
    }

    private static Status MakeStatusWithNoBranches(string state)
    {
        return MakeStatusWithBranches(state: state, branches: []);
    }

    private static Status MakeStatusWithBranches(string state, Branch[] branches)
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
            branches: branches,
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

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("error")]
    public async Task NonPendingStateWithNoBranchesShouldPublishAsync(string state)
    {
        (IMediator mediator, GithubStatusNotificationHandler handler) = this.CreateHandler();

        Status status = MakeStatusWithNoBranches(state: state);
        GithubStatus notification = new(status);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<string, uint?> StateColors =>
        new()
        {
            { "success", 0x2ECC71 },
            { "SUCCESS", 0x2ECC71 },
            { "failure", 0xE74C3C },
            { "error", null },
        };

    [Theory]
    [MemberData(nameof(StateColors))]
    public async Task PublishedEmbedHasExpectedContentAsync(string state, uint? expectedColor)
    {
        (IMediator mediator, GithubStatusNotificationHandler handler) = this.CreateHandler();

        await handler.Handle(notification: new(MakeStatus(state: state)), cancellationToken: this.CancellationToken());

        DiscordEmbed embed = mediator.PublishedEmbed();
        Assert.Equal(expected: "Build finished for ci/build from BuildBot (main)", actual: embed.Title);
        Assert.Equal(expected: new Uri("https://ci.example.com/builds/123"), actual: embed.Url);
        Assert.Equal(expected: "Built at abc123", actual: embed.Description);
        Assert.Equal(expected: expectedColor, actual: embed.Color?.Value);
        Assert.Equal(
            expected:
            [
                new DiscordEmbedField(Name: "Head commit", Value: "testuser - Fix something"),
                new DiscordEmbedField(Name: "Branch", Value: "main"),
            ],
            actual: embed.Fields
        );
    }

    [Fact]
    public async Task PublishedEmbedWithNoBranchesUsesUnknownAsync()
    {
        (IMediator mediator, GithubStatusNotificationHandler handler) = this.CreateHandler();

        await handler.Handle(
            notification: new(MakeStatusWithNoBranches(state: "success")),
            cancellationToken: this.CancellationToken()
        );

        DiscordEmbed embed = mediator.PublishedEmbed();
        Assert.Equal(expected: "Build finished for ci/build from BuildBot ((unknown))", actual: embed.Title);
        Assert.Equal(expected: new DiscordEmbedField(Name: "Branch", Value: "(unknown)"), actual: embed.Fields[1]);
    }
}
