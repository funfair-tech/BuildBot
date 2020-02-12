using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Commit
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "sha")]
        public string Sha { get; set; }

        [DataMember(Name = "tree_id")]
        public string TreeId { get; set; }

        [DataMember(Name = "distinct")]
        public bool Distinct { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "timestamp")]
        public DateTime TimeStamp { get; set; }

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "added")]
        public List<string> Added { get; set; }

        [DataMember(Name = "removed")]
        public List<string> Removed { get; set; }

        [DataMember(Name = "modified")]
        public List<string> Modified { get; set; }

        [DataMember(Name = "author")]
        public CommitUser Author { get; set; }

        [DataMember(Name = "committer")]
        public CommitUser Committer { get; set; }
    }
}