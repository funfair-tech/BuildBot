using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class Ping
    {
        [DataMember(Name = "zen")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Zen { get; set; } = default!;

        [DataMember(Name = "hook_id")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string HookId { get; set; } = default!;
    }
}