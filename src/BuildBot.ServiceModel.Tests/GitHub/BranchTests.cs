using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class BranchTests : TestBase
{
    [Fact]
    public void NamePropertyMatchesConstructorArgument()
    {
        Branch branch = new(name: "main");

        Assert.Equal(expected: "main", actual: branch.Name);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Branch first = new(name: "main");
        Branch second = new(name: "main");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        Branch first = new(name: "main");
        Branch second = new(name: "develop");

        Assert.NotEqual(expected: first, actual: second);
    }
}
