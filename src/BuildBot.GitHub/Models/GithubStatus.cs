using System.Diagnostics;
using BuildBot.ServiceModel.GitHub;
using Mediator;

namespace BuildBot.GitHub.Models;

[DebuggerDisplay("{Model.Context}")]
public sealed record GithubStatus(Status Model) : INotification;
