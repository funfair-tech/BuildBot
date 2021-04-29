using System.IO;
using Newtonsoft.Json;

namespace BuildBot.Discord
{
    public sealed class DiscordBotConfiguration
    {
        public string Token { get; init; } = default!;

        public string Server { get; init; } = default!;

        public string Channel { get; init; } = default!;

        public string ReleaseChannel { get; init; } = default!;

        public static DiscordBotConfiguration Load(string jsonFile)
        {
            return JsonConvert.DeserializeObject<DiscordBotConfiguration>(File.ReadAllText(jsonFile))!;
        }
    }
}