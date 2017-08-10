using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class StatusCommit
    {
        [DataMember(Name = "commit")]
        public Commit Commit { get; set; }

        [DataMember(Name = "sha")]
        public string Sha { get; set; }

        [DataMember(Name = "author")]
        public Author Author { get; set; }
    }
}
