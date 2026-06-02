using System.Threading.Tasks;
using BuildBot.GitHub.Models;
using BuildBot.GitHub.Publishers;
using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.GitHub.Tests.Publishers;

public sealed class GithubPingNotificationHandlerTests : TestBase
{
    [Fact]
    public async Task HandleLogsAndCompletesAsync()
    {
        ILogger<GithubPingNotificationHandler> logger = this.GetTypedLogger<GithubPingNotificationHandler>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        GithubPingNotificationHandler handler = new(logger);

        PingModel model = new(zen: "Keep it logically awesome.", hookId: "12345");
        GithubPing notification = new(model);

        await handler.Handle(notification: notification, cancellationToken: this.CancellationToken());

        logger.Received(1).IsEnabled(LogLevel.Information);
    }
}
