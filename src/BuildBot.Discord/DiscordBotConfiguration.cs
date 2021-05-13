using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace BuildBot.Discord
{
    [DebuggerDisplay("Server: {Server} Channel: {Channel} Release Channel: {ReleaseChannel}")]
    public sealed class DiscordBotConfiguration
    {
        [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Set by serialization")]
        public string Token { get; init; } = default!;

        [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Set by serialization")]
        public string Server { get; init; } = default!;

        [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Set by serialization")]
        public string Channel { get; init; } = default!;

        [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Set by serialization")]
        public string ReleaseChannel { get; init; } = default!;

        public static DiscordBotConfiguration Load(string jsonFile)
        {
            return JsonConvert.DeserializeObject<DiscordBotConfiguration>(File.ReadAllText(jsonFile))!;
        }
    }
}