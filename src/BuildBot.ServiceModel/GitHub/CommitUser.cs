using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class CommitUser
    {
        [DataMember(Name = "name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [DataMember(Name = "email")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Email { get; set; } = default!;

        [DataMember(Name = "username")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Username { get; set; } = default!;
    }
}