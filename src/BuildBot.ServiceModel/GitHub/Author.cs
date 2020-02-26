using System.Text.Json.Serialization;

namespace BuildBot.ServiceModel.GitHub
{
    public sealed class Author
    {
        [JsonPropertyName("login")]

        // ReSharper disable once RedundantDefaultMemberInitializer
        public string Login { get; set; } = default!;
    }
}