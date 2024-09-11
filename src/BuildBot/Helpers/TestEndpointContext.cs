using System.Diagnostics.CodeAnalysis;

namespace BuildBot.Helpers;

[SuppressMessage(category: "SonarAnalyzer.CSharp", checkId: "S2094: Remove empty class", Justification = "Needed for logging")]
public sealed class TestEndpointContext;