using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class Owner
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [DataMember(Name = "avatar_url")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string AvatarUrl { get; set; } = default!;
    }
}