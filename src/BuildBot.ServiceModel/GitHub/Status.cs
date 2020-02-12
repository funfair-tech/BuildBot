using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public sealed class Status
    {
        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [DataMember(Name = "target_url")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string TargetUrl { get; set; } = default!;

        [DataMember(Name = "repository")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Repository Repository { get; set; } = default!;

        [DataMember(Name = "context")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Context { get; set; } = default!;

        [DataMember(Name = "state")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string State { get; set; } = default!;

        [DataMember(Name = "branches")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<Branch> Branches { get; set; } = default!;

        [DataMember(Name = "description")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Description { get; set; } = default!;

        [DataMember(Name = "commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public StatusCommit StatusCommit { get; set; } = default!;
    }
}