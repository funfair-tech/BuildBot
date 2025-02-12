using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace BuildBot.GitHub.Models;

[DebuggerDisplay("{Model.Ref}")]
public sealed record GithubPush(Push Model) : INotification;
