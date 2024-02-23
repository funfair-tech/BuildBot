using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace MinimalApi.Messages;

[DebuggerDisplay("{Model.Ref}")]
public sealed record GithubPush(Push Model) : INotification;