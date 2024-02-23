using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace BuildBot.GitHub.Models;

[DebuggerDisplay("{Model.Zen}: {Model.HookId}")]
public sealed record GithubPing(PingModel Model) : INotification;