using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Id}: {Sha}: {Message}")]
public sealed class Commit
{
    [SuppressMessage(category: "Roslynator.Analyzers", checkId: "RCS1231: Make parameter ref read-only.", Justification = "Serialisation model")]
    [JsonConstructor]
    public Commit(string id,
                  string sha,
                  string treeId,
                  bool distinct,
                  string message,
                  DateTime timeStamp,
                  string url,
                  IReadOnlyList<string> added,
                  IReadOnlyList<string> removed,
                  IReadOnlyList<string> modified,
                  CommitUser author,
                  CommitUser committer)
    {
        this.Id = id;
        this.Sha = sha;
        this.TreeId = treeId;
        this.Distinct = distinct;
        this.Message = message;
        this.TimeStamp = timeStamp;
        this.Url = url;
        this.Added = added;
        this.Removed = removed;
        this.Modified = modified;
        this.Author = author;
        this.Committer = committer;
    }

    [JsonPropertyName(name: "id")]
    public string Id { get; }

    [JsonPropertyName(name: "sha")]
    public string Sha { get; }

    [JsonPropertyName(name: "tree_id")]
    public string TreeId { get; }

    [JsonPropertyName(name: "distinct")]
    public bool Distinct { get; }

    [JsonPropertyName(name: "message")]
    public string Message { get; }

    [JsonPropertyName(name: "timestamp")]
    public DateTime TimeStamp { get; }

    [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
    [JsonPropertyName(name: "url")]
    public string Url { get; }

    [JsonPropertyName(name: "added")]
    public IReadOnlyList<string> Added { get; }

    [JsonPropertyName(name: "removed")]
    public IReadOnlyList<string> Removed { get; }

    [JsonPropertyName(name: "modified")]
    public IReadOnlyList<string> Modified { get; }

    [JsonPropertyName(name: "author")]
    public CommitUser Author { get; }

    [JsonPropertyName(name: "committer")]

    public CommitUser Committer { get; }
}