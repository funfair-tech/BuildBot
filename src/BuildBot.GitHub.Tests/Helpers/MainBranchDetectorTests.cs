using BuildBot.GitHub.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Helpers;

public sealed class MainBranchDetectorTests : TestBase
{
    [Theory]
    [InlineData("main")]
    [InlineData("master")]
    public void ShouldBeMainBranch(string branch)
    {
        Assert.True(MainBranchDetector.IsRepoMainBranch(branch), userMessage: "Should be main branch");
    }

    [Theory]
    [InlineData("test")]
    [InlineData("feature/main")]
    [InlineData("fix/master")]
    [InlineData("mainly")]
    public void ShouldNotMainBranch(string branch)
    {
        Assert.False(MainBranchDetector.IsRepoMainBranch(branch), userMessage: "Should not be main branch");
    }
}
