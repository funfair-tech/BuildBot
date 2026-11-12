using System.Threading.Tasks;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.Tests;

public sealed class ProgramTests : TestBase
{
    [Fact]
    public async Task SelfTestSwitchExitsWithoutStartingServerAsync()
    {
        int exitCode = await Program.Main(["--self-test"]);

        Assert.Equal(expected: 0, actual: exitCode);
    }
}
