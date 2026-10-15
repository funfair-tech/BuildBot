using System.Diagnostics;

namespace BuildBot.GitHub.Middleware;

// Endpoint metadata marking the routes whose requests must carry a valid GitHub webhook signature.
[DebuggerDisplay("GitHub webhook endpoint")]
public sealed record GitHubWebhookEndpointMetadata;
