using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace MinimalApi.Messages;

[DebuggerDisplay("{PingModel.Zen}: {PingModel.HookId}")]
public sealed record GithubPing(PingModel PingModel) : INotification;