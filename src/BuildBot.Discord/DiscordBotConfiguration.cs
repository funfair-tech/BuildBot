using System.IO;

namespace BuildBot.Discord
{
    public sealed class DiscordBotConfiguration
    {
        public string Token { get; set; } = default!;

        public string Server { get; set; } = default!;

        public string Channel { get; set; } = default!;

        public string ReleaseChannel { get; set; } = default!;

        public static DiscordBotConfiguration Load(string jsonFile)
        {
            return JsonConvert.DeserializeObject<DiscordBotConfiguration>(File.ReadAllText(jsonFile));
        }
    }
}