using System.Text.Json.Serialization;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Json.Tests;

public sealed class AppSerializationContextTests : TestBase
{
    [Fact]
    public void DefaultMustNotBeNull()
    {
        JsonSerializerContext context = AppSerializationContext.Default;

        Assert.NotNull(context);
    }
}
