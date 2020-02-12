using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    [DataContract]
    public class Push
    {
        [DataMember(Name = "ref")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Ref { get; set; } = default!;

        [DataMember(Name = "before")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Before { get; set; } = default!;

        [DataMember(Name = "after")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string After { get; set; } = default!;

        [DataMember(Name = "head_commit")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Commit HeadCommit { get; set; } = default!;

        [DataMember(Name = "commits")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public List<Commit> Commits { get; set; } = default!;

        [DataMember(Name = "repository")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Repository Repository { get; set; } = default!;

        [DataMember(Name = "pusher")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Pusher Pusher { get; set; } = default!;

        [SuppressMessage(category: "Microsoft.Design", checkId: "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Model for serialization")]
        [DataMember(Name = "compare")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string CompareUrl { get; set; } = default!;
    }
}