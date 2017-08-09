using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Ping
    {
        [DataMember(Name = "zen")]
        public string Zen { get; set; }

        [DataMember(Name = "hook_id")]
        public string HookId { get; set; }
    }
}
