using BuildBot.Models;

namespace BuildBot.Helpers;

internal static class PingPong
{
    public static PongDto Model { get; } = new("Pong!");
}