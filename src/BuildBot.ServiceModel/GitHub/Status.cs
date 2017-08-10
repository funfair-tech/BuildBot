using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Status
    {
        [DataMember(Name = "target_url")]
        public string TargetUrl { get; set; }

        [DataMember(Name = "repository")]
        public Repository Repository { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "branches")]
        public List<Branch> Branches { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "commit")]
        public StatusCommit StatusCommit { get; set; }
    }
}
