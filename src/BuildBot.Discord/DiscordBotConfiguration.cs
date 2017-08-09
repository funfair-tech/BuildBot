using Newtonsoft.Json;
using System.IO;

namespace BuildBot.Discord
{
    public class DiscordBotConfiguration
    {
        public static DiscordBotConfiguration Load(string jsonFile)
        {
            return JsonConvert.DeserializeObject<DiscordBotConfiguration>(File.ReadAllText(jsonFile));
        }

        public string Token { get; set; }
        public string Server { get; set; }
        public string Channel { get; set; }
    }
}
