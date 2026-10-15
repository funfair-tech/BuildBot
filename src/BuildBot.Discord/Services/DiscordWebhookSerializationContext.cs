using System.Text.Json.Serialization;
using BuildBot.Discord.Services.Models;

namespace BuildBot.Discord.Services;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    IncludeFields = false
)]
[JsonSerializable(typeof(WebhookPayload))]
[JsonSerializable(typeof(RateLimitResponse))]
internal sealed partial class DiscordWebhookSerializationContext : JsonSerializerContext;
