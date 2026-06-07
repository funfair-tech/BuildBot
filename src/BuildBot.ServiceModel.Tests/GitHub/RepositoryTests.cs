using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class RepositoryTests : TestBase
{
    private static readonly Owner Owner = new(avatarUrl: "https://example.com/avatar.png");

    [Fact]
    public void IdPropertyMatchesConstructorArgument()
    {
        Repository repo = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);

        Assert.Equal(expected: 42, actual: repo.Id);
    }

    [Fact]
    public void NamePropertyMatchesConstructorArgument()
    {
        Repository repo = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);

        Assert.Equal(expected: "BuildBot", actual: repo.Name);
    }

    [Fact]
    public void FullNamePropertyMatchesConstructorArgument()
    {
        Repository repo = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);

        Assert.Equal(expected: "funfair-tech/BuildBot", actual: repo.FullName);
    }

    [Fact]
    public void OwnerPropertyMatchesConstructorArgument()
    {
        Repository repo = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);

        Assert.Equal(expected: Owner, actual: repo.Owner);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Repository first = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);
        Repository second = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        Repository first = new(id: 42, name: "BuildBot", fullName: "funfair-tech/BuildBot", owner: Owner);
        Repository second = new(id: 99, name: "OtherRepo", fullName: "funfair-tech/OtherRepo", owner: Owner);

        Assert.NotEqual(expected: first, actual: second);
    }
}
