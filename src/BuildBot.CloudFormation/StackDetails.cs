using System.Diagnostics;

namespace BuildBot.CloudFormation;

[DebuggerDisplay("{Description} {Version}")]
public readonly record struct StackDetails(string? Description, string? Version);