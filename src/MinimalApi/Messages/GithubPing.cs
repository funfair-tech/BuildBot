using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace MinimalApi.Messages;

[DebuggerDisplay("{Model.Zen}: {Model.HookId}")]
public sealed record GithubPing(PingModel Model) : INotification;