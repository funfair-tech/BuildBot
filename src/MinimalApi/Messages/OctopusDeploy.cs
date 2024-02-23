using System.Diagnostics;
using BuildBot.ServiceModel.Octopus;
using Mediator;

namespace MinimalApi.Messages;

[DebuggerDisplay("{Model.Timestamp}")]
public sealed record OctopusDeploy(Deploy Model) : INotification;