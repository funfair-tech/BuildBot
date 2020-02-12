using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class Author
    {
        [DataMember(Name = "login")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Login { get; set; } = default!;
    }
}