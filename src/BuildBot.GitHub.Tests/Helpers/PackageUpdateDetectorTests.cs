using BuildBot.GitHub.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.GitHub.Tests.Helpers;

public sealed class PackageUpdateDetectorTests : TestBase
{
    [Theory]
    [InlineData("FF-1429 -  Updated package")]
    [InlineData("[FF-1429] - Updated something else")]
    [InlineData("Dependencies - Updated package")]
    [InlineData("[Dependencies]  - Updated something else")]
    public void ShouldBeConsideredAPackageUpdate(string commitMessage)
    {
        Assert.True(PackageUpdateDetector.IsPackageUpdate(commitMessage), userMessage: "Should be a package update");
    }

    [Theory]
    [InlineData("Introduced FF-1429")]
    [InlineData("Introduced FF-1429 detection")]
    [InlineData("Test [FF-1429]")]
    [InlineData("Test [FF-1429] Detection")]
    [InlineData("Validating Dependencies")]
    [InlineData("Validating Dependencies of packages")]
    [InlineData("Identifying [Dependencies] too")]
    public void ShouldNotBeConsideredAPackageUpdate(string commitMessage)
    {
        Assert.False(PackageUpdateDetector.IsPackageUpdate(commitMessage), userMessage: "Should not be a package update");
    }
}