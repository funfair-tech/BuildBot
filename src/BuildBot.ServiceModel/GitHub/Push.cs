using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Ref}: {Repository.FullName}")]
public readonly record struct Push
{
    [JsonConstructor]
    public Push(string @ref, string before, string after, in Commit headCommit, IReadOnlyList<Commit> commits, in Repository repository, in Pusher pusher, string compareUrl)
    {
        this.Ref = @ref;
        this.Before = before;
        this.After = after;
        this.HeadCommit = headCommit;
        this.Commits = commits;
        this.Repository = repository;
        this.Pusher = pusher;
        this.CompareUrl = compareUrl;
    }

    [JsonPropertyName(name: "ref")]

    public string Ref { get; }

    [JsonPropertyName(name: "before")]

    public string Before { get; }

    [JsonPropertyName(name: "after")]

    public string After { get; }

    [JsonPropertyName(name: "head_commit")]

    public Commit HeadCommit { get; }

    [JsonPropertyName(name: "commits")]

    public IReadOnlyList<Commit> Commits { get; }

    [JsonPropertyName(name: "repository")]

    public Repository Repository { get; }

    [JsonPropertyName(name: "pusher")]

    public Pusher Pusher { get; }

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [JsonPropertyName(name: "compare")]

    public string CompareUrl { get; }
}