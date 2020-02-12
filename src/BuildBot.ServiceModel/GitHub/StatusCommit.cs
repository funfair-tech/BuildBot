using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class StatusCommit
    {
        [DataMember(Name = "commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Commit Commit { get; set; } = default!;

        [DataMember(Name = "sha")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Sha { get; set; } = default!;

        [DataMember(Name = "author")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Author Author { get; set; } = default!;
    }
}