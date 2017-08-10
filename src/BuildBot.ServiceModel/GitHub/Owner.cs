using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Owner
    {
        [DataMember(Name = "avatar_url")]
        public string AvatarUrl { get; set; }
    }
}
