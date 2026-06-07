using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class AuthorTests : TestBase
{
    [Fact]
    public void LoginPropertyMatchesConstructorArgument()
    {
        Author author = new(login: "octocat");

        Assert.Equal(expected: "octocat", actual: author.Login);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        Author first = new(login: "octocat");
        Author second = new(login: "octocat");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        Author first = new(login: "octocat");
        Author second = new(login: "hubot");

        Assert.NotEqual(expected: first, actual: second);
    }
}
