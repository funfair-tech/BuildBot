using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Push
    {
        [DataMember(Name = "ref")]
        public string Ref { get; set; }

        [DataMember(Name = "before")]
        public string Before { get; set; }

        [DataMember(Name = "after")]
        public string After { get; set; }

        [DataMember(Name = "head_commit")]
        public Commit HeadCommit { get; set; }

        [DataMember(Name = "commits")]
        public List<Commit> Commits { get; set; }

        [DataMember(Name = "repository")]
        public Repository Repository { get; set; }

        [DataMember(Name = "pusher")]
        public Pusher Pusher { get; set; }

        [DataMember(Name = "compare")]
        public string CompareUrl { get; set; }
    }
}
