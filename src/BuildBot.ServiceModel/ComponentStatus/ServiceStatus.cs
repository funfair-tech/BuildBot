using System.Diagnostics;

namespace BuildBot.ServiceModel.ComponentStatus;

[DebuggerDisplay("{Name}: {Ok}")]
public readonly record struct ServiceStatus(string Name, bool Ok);
