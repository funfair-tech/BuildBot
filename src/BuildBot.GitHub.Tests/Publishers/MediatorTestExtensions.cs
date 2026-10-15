using System.Linq;
using BuildBot.Discord.Models;
using Mediator;
using NSubstitute;

namespace BuildBot.GitHub.Tests.Publishers;

internal static class MediatorTestExtensions
{
    public static DiscordEmbed PublishedEmbed(this IMediator mediator)
    {
        return mediator
            .ReceivedCalls()
            .Select(static call => call.GetArguments()[0])
            .OfType<BotMessage>()
            .Single()
            .Message;
    }
}
