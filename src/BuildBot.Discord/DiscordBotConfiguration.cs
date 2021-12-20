using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace BuildBot.Discord;

public sealed class DiscordBotConfiguration
{
    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Used by serialisation")]
    public string Token { get; init; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Used by serialisation")]
    public string Server { get; init; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Used by serialisation")]
    public string Channel { get; init; } = default!;

    [SuppressMessage(category: "ReSharper", checkId: "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Used by serialisation")]
    public string ReleaseChannel { get; init; } = default!;

    public static DiscordBotConfiguration Load(string jsonFile)
    {
        return JsonConvert.DeserializeObject<DiscordBotConfiguration>(File.ReadAllText(jsonFile))!;
    }
}