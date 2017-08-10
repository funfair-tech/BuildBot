using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Branch
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
