using System.Diagnostics;

namespace BuildBot.CloudFormation;

[DebuggerDisplay("{StackName} {Project} {Arn} {Status} {Success}")]
public readonly record struct Deployment(string StackName, string Project, string Arn, string Status, bool Success, string StackId);