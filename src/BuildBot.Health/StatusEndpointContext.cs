using System.Diagnostics.CodeAnalysis;

namespace BuildBot.Health;

[SuppressMessage(
    category: "SonarAnalyzer.CSharp",
    checkId: "S2094: Remove empty class",
    Justification = "Needed for logging"
)]
public sealed class StatusEndpointContext;
