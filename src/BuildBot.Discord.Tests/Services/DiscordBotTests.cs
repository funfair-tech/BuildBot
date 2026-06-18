using System;
using System.Threading.Tasks;
using BuildBot.Discord.Services;
using BuildBot.ServiceModel.ComponentStatus;
using Discord;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.Discord.Tests.Services;

public sealed class DiscordBotTests : TestBase
{
    private static readonly DiscordBotConfiguration Config = new(
        token: "test-token",
        server: "test-server",
        channel: "test-channel",
        releaseChannel: "test-release-channel"
    );

    private (IDiscordRawClient Client, ILogger<DiscordBot> Logger, DiscordBot Bot) CreateBot()
    {
        IDiscordRawClient client = GetSubstitute<IDiscordRawClient>();
        ILogger<DiscordBot> logger = this.GetTypedLogger<DiscordBot>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        DiscordBot bot = new(client: client, botConfiguration: Config, logger: logger);

        return (client, logger, bot);
    }

    private (ILogger<DiscordBot> Logger, Func<LogMessage, Task> Handler) CreateBotForLogTests()
    {
        IDiscordRawClient client = GetSubstitute<IDiscordRawClient>();
        ILogger<DiscordBot> logger = this.GetTypedLogger<DiscordBot>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Func<LogMessage, Task>? capturedHandler = null;
        client
            .When(x => x.RegisterLogHandler(Arg.Any<Func<LogMessage, Task>>()))
            .Do(callInfo => capturedHandler = callInfo.Arg<Func<LogMessage, Task>>());

        _ = new DiscordBot(client: client, botConfiguration: Config, logger: logger);

        return (
            logger,
            capturedHandler
                ?? throw new InvalidOperationException("DiscordBot constructor did not call RegisterLogHandler")
        );
    }

    [Fact]
    public void GetStatus_WhenLoggedOut_ReturnsDisconnectedStatus()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        client.LoginState.Returns(LoginState.LoggedOut);

        ServiceStatus status = bot.GetStatus();

        Assert.Equal(expected: "Discord", actual: status.Name);
        Assert.False(condition: status.Ok, userMessage: "Discord should not be connected when logged out");
    }

    [Fact]
    public void GetStatus_WhenLoggedIn_ReturnsConnectedStatus()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        client.LoginState.Returns(LoginState.LoggedIn);

        ServiceStatus status = bot.GetStatus();

        Assert.Equal(expected: "Discord", actual: status.Name);
        Assert.True(condition: status.Ok, userMessage: "Discord should be connected when logged in");
    }

    [Fact]
    public async Task PublishAsync_WhenChannelNotFound_LogsErrorAndReturns()
    {
        (IDiscordRawClient client, ILogger<DiscordBot> logger, DiscordBot bot) = this.CreateBot();

        client
            .FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>())
            .Returns((IDiscordChannel?)null);

        await bot.PublishAsync(
            builder: new EmbedBuilder().WithTitle("Test"),
            cancellationToken: this.CancellationToken()
        );

        client.Received(1).FindChannel(serverName: Config.Server, channelName: Config.Channel);
        logger.Received(1).IsEnabled(LogLevel.Error);
    }

    [Fact]
    public async Task PublishToReleaseChannelAsync_WhenChannelNotFound_LogsErrorAndReturns()
    {
        (IDiscordRawClient client, ILogger<DiscordBot> logger, DiscordBot bot) = this.CreateBot();

        client
            .FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>())
            .Returns((IDiscordChannel?)null);

        await bot.PublishToReleaseChannelAsync(
            builder: new EmbedBuilder().WithTitle("Test"),
            cancellationToken: this.CancellationToken()
        );

        client.Received(1).FindChannel(serverName: Config.Server, channelName: Config.ReleaseChannel);
        logger.Received(1).IsEnabled(LogLevel.Error);
    }

    [Fact]
    public async Task PublishAsync_WhenChannelFound_SendsMessageAndLogsSuccess()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        IDiscordChannel mockChannel = GetSubstitute<IDiscordChannel>();
        IDisposable mockTypingState = GetSubstitute<IDisposable>();

        mockChannel.Name.Returns("test-channel");
        mockChannel.EnterTypingState().Returns(mockTypingState);
        mockChannel
            .SendMessageAsync(Arg.Any<Embed>())
            .Returns(Task.FromResult<(string SentToChannel, string MessageContent)>(("test-channel", string.Empty)));

        client.FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>()).Returns(mockChannel);

        await bot.PublishAsync(
            builder: new EmbedBuilder().WithTitle("Test Message"),
            cancellationToken: this.CancellationToken()
        );

        await mockChannel.Received(1).SendMessageAsync(Arg.Any<Embed>());
    }

    [Fact]
    public async Task PublishToReleaseChannelAsync_WhenChannelFound_SendsMessage()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        IDiscordChannel mockChannel = GetSubstitute<IDiscordChannel>();
        IDisposable mockTypingState = GetSubstitute<IDisposable>();

        mockChannel.Name.Returns("test-release-channel");
        mockChannel.EnterTypingState().Returns(mockTypingState);
        mockChannel
            .SendMessageAsync(Arg.Any<Embed>())
            .Returns(
                Task.FromResult<(string SentToChannel, string MessageContent)>(("test-release-channel", string.Empty))
            );

        client.FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>()).Returns(mockChannel);

        await bot.PublishToReleaseChannelAsync(
            builder: new EmbedBuilder().WithTitle("Release Test"),
            cancellationToken: this.CancellationToken()
        );

        await mockChannel.Received(1).SendMessageAsync(Arg.Any<Embed>());
    }

    [Fact]
    public async Task PublishAsync_WhenSendFails_WhileLoggedIn_ReconnectsAfterLogout()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        IDiscordChannel mockChannel = GetSubstitute<IDiscordChannel>();
        IDisposable mockTypingState = GetSubstitute<IDisposable>();

        mockChannel.Name.Returns("test-channel");
        mockChannel.EnterTypingState().Returns(mockTypingState);
        mockChannel
            .SendMessageAsync(Arg.Any<Embed>())
            .Returns(
                Task.FromException<(string SentToChannel, string MessageContent)>(
                    new InvalidOperationException("send failed")
                )
            );

        client.FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>()).Returns(mockChannel);
        client.LoginState.Returns(LoginState.LoggedIn);

        await bot.PublishAsync(
            builder: new EmbedBuilder().WithTitle("Test"),
            cancellationToken: this.CancellationToken()
        );

        await client.Received(1).LogoutAsync();
        await client.Received(1).StopAsync();
        await client.Received(1).LoginAsync(tokenType: TokenType.Bot, token: Config.Token);
    }

    [Fact]
    public async Task PublishAsync_WhenSendFails_WhileNotLoggedIn_ReconnectsWithoutLogout()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        IDiscordChannel mockChannel = GetSubstitute<IDiscordChannel>();
        IDisposable mockTypingState = GetSubstitute<IDisposable>();

        mockChannel.Name.Returns("test-channel");
        mockChannel.EnterTypingState().Returns(mockTypingState);
        mockChannel
            .SendMessageAsync(Arg.Any<Embed>())
            .Returns(
                Task.FromException<(string SentToChannel, string MessageContent)>(
                    new InvalidOperationException("send failed")
                )
            );

        client.FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>()).Returns(mockChannel);
        client.LoginState.Returns(LoginState.LoggedOut);

        await bot.PublishAsync(
            builder: new EmbedBuilder().WithTitle("Test"),
            cancellationToken: this.CancellationToken()
        );

        await client.DidNotReceive().LogoutAsync();
        await client.Received(1).LoginAsync(tokenType: TokenType.Bot, token: Config.Token);
    }

    [Fact]
    public async Task PublishAsync_WhenReconnectFails_LogsError()
    {
        (IDiscordRawClient client, ILogger<DiscordBot> logger, DiscordBot bot) = this.CreateBot();

        IDiscordChannel mockChannel = GetSubstitute<IDiscordChannel>();
        IDisposable mockTypingState = GetSubstitute<IDisposable>();

        mockChannel.Name.Returns("test-channel");
        mockChannel.EnterTypingState().Returns(mockTypingState);
        mockChannel
            .SendMessageAsync(Arg.Any<Embed>())
            .Returns(
                Task.FromException<(string SentToChannel, string MessageContent)>(
                    new InvalidOperationException("send failed")
                )
            );

        client.FindChannel(serverName: Arg.Any<string>(), channelName: Arg.Any<string>()).Returns(mockChannel);
        client.LoginState.Returns(LoginState.LoggedIn);
        client.LogoutAsync().Returns(Task.FromException(new InvalidOperationException("logout failed")));

        await bot.PublishAsync(
            builder: new EmbedBuilder().WithTitle("Test"),
            cancellationToken: this.CancellationToken()
        );

        logger.Received(2).IsEnabled(LogLevel.Error);
    }

    [Fact]
    public async Task StartAsync_CallsLoginStartAndSetGame()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        await bot.StartAsync(this.CancellationToken());

        await client.Received(1).LoginAsync(tokenType: TokenType.Bot, token: Config.Token);
        await client.Received(1).StartAsync();
        await client.Received(1).SetGameAsync(name: "GitHub", streamUrl: null, type: ActivityType.Watching);
    }

    [Fact]
    public async Task StopAsync_CallsLogout()
    {
        (IDiscordRawClient client, _, DiscordBot bot) = this.CreateBot();

        await bot.StopAsync(this.CancellationToken());

        await client.Received(1).LogoutAsync();
    }

    [Fact]
    public async Task LogAsync_Debug_LogsAtDebugLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage(LogSeverity.Debug, source: "Test", message: "Debug message"));

        logger.Received(1).IsEnabled(LogLevel.Debug);
    }

    [Fact]
    public async Task LogAsync_Verbose_LogsAtInformationLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage(LogSeverity.Verbose, source: "Test", message: "Verbose message"));

        logger.Received(1).IsEnabled(LogLevel.Information);
    }

    [Fact]
    public async Task LogAsync_Info_LogsAtInformationLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage(LogSeverity.Info, source: "Test", message: "Info message"));

        logger.Received(1).IsEnabled(LogLevel.Information);
    }

    [Fact]
    public async Task LogAsync_Warning_LogsAtWarningLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage(LogSeverity.Warning, source: "Test", message: "Warning message"));

        logger.Received(1).IsEnabled(LogLevel.Warning);
    }

    [Fact]
    public async Task LogAsync_ErrorWithoutException_LogsAtErrorLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage(LogSeverity.Error, source: "Test", message: "Error message"));

        logger.Received(1).IsEnabled(LogLevel.Error);
    }

    [Fact]
    public async Task LogAsync_ErrorWithException_LogsAtErrorLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(
            new LogMessage(
                LogSeverity.Error,
                source: "Test",
                message: "Error message",
                exception: new InvalidOperationException("test error")
            )
        );

        logger.Received(1).IsEnabled(LogLevel.Error);
    }

    [Fact]
    public async Task LogAsync_CriticalWithoutException_LogsAtCriticalLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage(LogSeverity.Critical, source: "Test", message: "Critical message"));

        logger.Received(1).IsEnabled(LogLevel.Critical);
    }

    [Fact]
    public async Task LogAsync_CriticalWithException_LogsAtCriticalLevel()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(
            new LogMessage(
                LogSeverity.Critical,
                source: "Test",
                message: "Critical message",
                exception: new InvalidOperationException("critical error")
            )
        );

        logger.Received(1).IsEnabled(LogLevel.Critical);
    }

    [Fact]
    public async Task LogAsync_UnknownSeverity_LogsAsCritical()
    {
        (ILogger<DiscordBot> logger, Func<LogMessage, Task> handler) = this.CreateBotForLogTests();

        await handler(new LogMessage((LogSeverity)99, source: "Test", message: "Unknown severity message"));

        logger.Received(1).IsEnabled(LogLevel.Critical);
    }
}
