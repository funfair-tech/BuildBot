using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Repository
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;

        [JsonPropertyName("full_name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string FullName { get; set; } = default!;

        [JsonPropertyName("owner")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public Owner Owner { get; set; } = default!;
    }
}