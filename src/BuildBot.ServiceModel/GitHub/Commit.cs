using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class Commit
    {
        [DataMember(Name = "id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Id { get; set; } = default!;

        [DataMember(Name = "sha")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Sha { get; set; } = default!;

        [DataMember(Name = "tree_id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string TreeId { get; set; } = default!;

        [DataMember(Name = "distinct")]
        public bool Distinct { get; set; }

        [DataMember(Name = "message")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Message { get; set; } = default!;

        [DataMember(Name = "timestamp")]
        public DateTime TimeStamp { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [DataMember(Name = "url")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Url { get; set; } = default!;

        [DataMember(Name = "added")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Added { get; set; } = default!;

        [DataMember(Name = "removed")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Removed { get; set; } = default!;

        [DataMember(Name = "modified")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<string> Modified { get; set; } = default!;

        [DataMember(Name = "author")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public CommitUser Author { get; set; } = default!;

        [DataMember(Name = "committer")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public CommitUser Committer { get; set; } = default!;
    }
}