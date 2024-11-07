using System.Collections.Generic;
using System.Diagnostics;
using BuildBot.ServiceModel.ComponentStatus;
using Mediator;

namespace BuildBot.Health;

[DebuggerDisplay("Source: {Source}")]
public readonly record struct CheckStatus(string? Source) : ICommand<IReadOnlyList<ServiceStatus>>;