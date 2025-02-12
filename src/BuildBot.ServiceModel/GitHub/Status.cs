using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Repository.FullName}: {Context}: {State}")]
public readonly record struct Status
{
    [JsonConstructor]
    public Status(
        string targetUrl,
        in Repository repository,
        string context,
        string state,
        IReadOnlyList<Branch> branches,
        string description,
        in StatusCommit statusCommit
    )
    {
        this.TargetUrl = targetUrl;
        this.Repository = repository;
        this.Context = context;
        this.State = state;
        this.Branches = branches;
        this.Description = description;
        this.StatusCommit = statusCommit;
    }

    [SuppressMessage(
        category: "Microsoft.Design",
        checkId: "CA1056:UriPropertiesShouldNotBeStrings",
        Justification = "Model for serialization"
    )]
    [JsonPropertyName(name: "target_url")]
    public string TargetUrl { get; }

    [JsonPropertyName(name: "repository")]
    public Repository Repository { get; }

    [JsonPropertyName(name: "context")]
    public string Context { get; }

    [JsonPropertyName(name: "state")]
    public string State { get; }

    [JsonPropertyName(name: "branches")]
    public IReadOnlyList<Branch> Branches { get; }

    [JsonPropertyName(name: "description")]
    public string Description { get; }

    [JsonPropertyName(name: "commit")]
    public StatusCommit StatusCommit { get; }
}
