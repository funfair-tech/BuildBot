using BuildBot.Models;

namespace MinimalApi.Helpers;

internal static class PingPong
{
    public static PongDto Model { get; } = new("Pong!");
}