using System;
using System.Collections.Generic;
using System.Linq;
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

public sealed class GithubPushNotificationHandlerTests : TestBase
{
    private const string NORMAL_REF = "refs/heads/feature/my-feature";
    private const string MAIN_REF = "refs/heads/main";
    private const string TEAMCITY_REPO = "TeamCity";
    private const string NORMAL_REPO = "BuildBot";
    private static readonly DateTime COMMIT_TIMESTAMP = FunFair
        .Test.Infrastructure.Mocks.MockDateTimeSources.Past.GetUtcNow()
        .UtcDateTime;

    private static Commit MakeCommit(string message, string? username = "testuser")
    {
        return new Commit(
            id: "abc123",
            sha: "abc123",
            treeId: "tree123",
            distinct: true,
            message: message,
            timeStamp: COMMIT_TIMESTAMP,
            url: "https://github.com/example/repo/commit/abc123",
            added: [],
            removed: [],
            modified: [],
            author: new CommitUser(name: "Test User", email: "test@example.com", username: username),
            committer: new CommitUser(name: "Test User", email: "test@example.com", username: username)
        );
    }

    private static Push MakePush(string repoName, string gitRef, IReadOnlyList<Commit> commits)
    {
        Commit headCommit = commits.Count > 0 ? commits[0] : MakeCommit("initial");
        Repository repository = new(
            id: 1,
            name: repoName,
            fullName: $"funfair-tech/{repoName}",
            owner: new Owner(avatarUrl: "https://example.com/avatar.png")
        );
        Pusher pusher = new(name: "testpusher", email: "push@example.com");

        return new Push(
            @ref: gitRef,
            before: "before-sha",
            after: "after-sha",
            headCommit: headCommit,
            commits: commits,
            repository: repository,
            pusher: pusher,
            compareUrl: "https://github.com/example/repo/compare/before...after"
        );
    }

    private (IMediator mediator, GithubPushNotificationHandler handler) CreateHandler()
    {
        IMediator mediator = GetSubstitute<IMediator>();
        ILogger<GithubPushNotificationHandler> logger = this.GetTypedLogger<GithubPushNotificationHandler>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return (mediator, new GithubPushNotificationHandler(mediator: mediator, logger: logger));
    }

    [Fact]
    public async Task ZeroCommitsShouldNotPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: []);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TeamCityRepoShouldNotPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit = MakeCommit("Normal commit");
        Push push = MakePush(repoName: TEAMCITY_REPO, gitRef: NORMAL_REF, commits: [commit]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleChoreCommitShouldNotPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit = MakeCommit("chore: update dependencies");
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: [commit]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SinglePackageUpdateOnMainBranchShouldNotPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit = MakeCommit("FF-1429 - Updated packages");
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: MAIN_REF, commits: [commit]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.DidNotReceive().Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SinglePackageUpdateOnFeatureBranchShouldPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit = MakeCommit("FF-1429 - Updated packages");
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: [commit]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleNonIgnoredCommitOnMainBranchShouldPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit = MakeCommit(message: "Fix bug in handler", username: "devuser");
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: MAIN_REF, commits: [commit]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TwoNormalCommitsShouldPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit1 = MakeCommit("First commit");
        Commit commit2 = MakeCommit("Second commit");
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: [commit1, commit2]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SingleCommitWithNullUsernameShouldPublishAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit = MakeCommit(message: "Normal commit with no github username", username: null);
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: [commit]);
        GithubPush notification = new(push);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        await mediator.Received(1).Publish(Arg.Any<BotMessage>(), Arg.Any<CancellationToken>());
    }

    private static DiscordEmbed PublishedEmbed(IMediator mediator)
    {
        return mediator
            .ReceivedCalls()
            .Select(static call => call.GetArguments()[0])
            .OfType<BotMessage>()
            .Single()
            .Message;
    }

    [Fact]
    public async Task PublishedEmbedHasExpectedContentAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Commit commit1 = MakeCommit(message: "First commit", username: "devuser");
        Commit commit2 = MakeCommit(message: "Second commit", username: null);
        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: [commit1, commit2]);

        await handler.Handle(notification: new(push), cancellationToken: this.CancellationToken());

        DiscordEmbed embed = PublishedEmbed(mediator);
        Assert.Equal(expected: "testpusher pushed 2 commits to BuildBot (feature/my-feature)", actual: embed.Title);
        Assert.Equal(expected: new Uri("https://github.com/example/repo/compare/before...after"), actual: embed.Url);
        Assert.Null(embed.Description);
        Assert.Null(embed.Colour);
        Assert.Equal(
            expected:
            [
                new DiscordEmbedField(Name: "**devuser** - First commit", Value: "0 added, 0 modified, 0 removed"),
                new DiscordEmbedField(Name: "**Test User** - Second commit", Value: "0 added, 0 modified, 0 removed"),
            ],
            actual: embed.Fields
        );
    }

    [Fact]
    public async Task PublishedEmbedForSingleCommitUsesSingularTitleAsync()
    {
        (IMediator mediator, GithubPushNotificationHandler handler) = this.CreateHandler();

        Push push = MakePush(repoName: NORMAL_REPO, gitRef: NORMAL_REF, commits: [MakeCommit("Only commit")]);

        await handler.Handle(notification: new(push), cancellationToken: this.CancellationToken());

        Assert.Equal(
            expected: "testpusher pushed 1 commit to BuildBot (feature/my-feature)",
            actual: PublishedEmbed(mediator).Title
        );
    }
}
