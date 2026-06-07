using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BuildBot.Json.Models;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Json.Tests;

public sealed class AppSerializationContextTests : TestBase
{
    [Fact]
    public void CanRoundTripPongDtoViaDefaultContext()
    {
        PongDto original = new("pong");
        JsonTypeInfo<PongDto> typeInfo = AssertReallyNotNull(
            AppSerializationContext.Default.GetTypeInfo(typeof(PongDto)) as JsonTypeInfo<PongDto>
        );

        string json = JsonSerializer.Serialize(original, typeInfo);
        PongDto deserialized = JsonSerializer.Deserialize(json, typeInfo);

        Assert.Equal(expected: original, actual: deserialized);
    }
}
