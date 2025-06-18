using System.Diagnostics;

namespace BuildBot.ServiceModel.Watchtower;

[DebuggerDisplay("{Title} => {Message}")]
public readonly record struct WatchTowerMessage(string Message, string Title);