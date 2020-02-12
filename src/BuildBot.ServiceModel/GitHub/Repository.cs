using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class Repository
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [DataMember(Name = "full_name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string FullName { get; set; } = default!;

        [DataMember(Name = "owner")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Owner Owner { get; set; } = default!;
    }
}