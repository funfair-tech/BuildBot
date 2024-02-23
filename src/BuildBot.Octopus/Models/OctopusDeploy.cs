using System.Diagnostics;
using BuildBot.ServiceModel.Octopus;
using Mediator;

namespace BuildBot.Octopus.Models;

[DebuggerDisplay("{Model.Timestamp}")]
public sealed record OctopusDeploy(Deploy Model) : INotification;