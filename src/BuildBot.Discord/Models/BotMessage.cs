using System.Diagnostics;
using Discord;
using Mediator;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Message.Title}")]
public sealed record BotMessage(Embed Message) : INotification;