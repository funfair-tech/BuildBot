using System.Diagnostics;
using System.Net;

namespace BuildBot.Discord.Services.Models;

[DebuggerDisplay("Success: {Success}, Status: {StatusCode}")]
public readonly record struct WebhookSendResult(bool Success, HttpStatusCode StatusCode);
