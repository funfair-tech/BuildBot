using System.Diagnostics;
using BuildBot.ServiceModel.Watchtower;
using Mediator;

namespace BuildBot.Watchtower.Models;

[DebuggerDisplay("{Model.Message}")]
public sealed record WatchTowerPublishMessage(WatchTowerMessage Model) : INotification;