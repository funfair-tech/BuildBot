using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Branch
    {
        [JsonPropertyName("name")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Name { get; set; } = default!;
    }
}