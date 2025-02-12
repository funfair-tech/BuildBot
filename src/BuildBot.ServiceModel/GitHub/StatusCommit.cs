using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

[DebuggerDisplay("{Sha}: {Author.Login}: {Commit.Message}")]
public readonly record struct StatusCommit
{
    [JsonConstructor]
    public StatusCommit(in Commit commit, string sha, in Author author)
    {
        this.Commit = commit;
        this.Sha = sha;
        this.Author = author;
    }

    [JsonPropertyName(name: "commit")]
    public Commit Commit { get; }

    [JsonPropertyName(name: "sha")]
    public string Sha { get; }

    [JsonPropertyName(name: "author")]
    public Author Author { get; }
}
