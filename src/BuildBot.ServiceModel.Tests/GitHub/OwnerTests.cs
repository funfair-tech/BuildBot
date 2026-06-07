using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class OwnerTests : TestBase
{
    [Fact]
    public void AvatarUrlPropertyMatchesConstructorArgument()
    {
        Owner owner = new(avatarUrl: "https://example.com/avatar.png");

        Assert.Equal(expected: "https://example.com/avatar.png", actual: owner.AvatarUrl);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Owner first = new(avatarUrl: "https://example.com/avatar.png");
        Owner second = new(avatarUrl: "https://example.com/avatar.png");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        Owner first = new(avatarUrl: "https://example.com/avatar.png");
        Owner second = new(avatarUrl: "https://example.com/other.png");

        Assert.NotEqual(expected: first, actual: second);
    }
}
