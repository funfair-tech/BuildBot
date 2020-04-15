using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Repository
    {
        [JsonPropertyName(name: "id")]
        public int Id { get; set; }

        [JsonPropertyName(name: "name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [JsonPropertyName(name: "full_name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string FullName { get; set; } = default!;

        [JsonPropertyName(name: "owner")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Owner Owner { get; set; } = default!;
    }
}