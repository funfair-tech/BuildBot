using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace MinimalApi.Messages;

[DebuggerDisplay("{Model.Context}")]
public sealed record GithubStatus(Status Model) : INotification;