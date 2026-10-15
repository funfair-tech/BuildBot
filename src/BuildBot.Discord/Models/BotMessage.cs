using System.Diagnostics;
using Mediator;

namespace BuildBot.Discord.Models;

[DebuggerDisplay("{Message.Title}")]
public sealed record BotMessage(DiscordEmbed Message) : INotification;
