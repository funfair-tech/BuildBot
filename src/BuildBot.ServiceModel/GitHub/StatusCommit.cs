using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub;

public sealed class StatusCommit
{
    [JsonConstructor]
    public StatusCommit(Commit commit, string sha, Author author)
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