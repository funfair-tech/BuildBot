using BuildBot.ServiceModel.GitHub;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.ServiceModel.Tests.GitHub;

public sealed class CommitUserTests : TestBase
{
    [Fact]
    public void NamePropertyMatchesConstructorArgument()
    {
        CommitUser user = new(name: "Test User", email: "test@example.com", username: "testuser");

        Assert.Equal(expected: "Test User", actual: user.Name);
    }

    [Fact]
    public void EmailPropertyMatchesConstructorArgument()
    {
        CommitUser user = new(name: "Test User", email: "test@example.com", username: "testuser");

        Assert.Equal(expected: "test@example.com", actual: user.Email);
    }

    [Fact]
    public void UsernamePropertyMatchesConstructorArgumentWhenProvided()
    {
        CommitUser user = new(name: "Test User", email: "test@example.com", username: "testuser");

        Assert.Equal(expected: "testuser", actual: user.Username);
    }

    [Fact]
    public void UsernamePropertyIsNullWhenNotProvided()
    {
        CommitUser user = new(name: "Test User", email: "test@example.com", username: null);

        Assert.Null(user.Username);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        CommitUser first = new(name: "Test User", email: "test@example.com", username: "testuser");
        CommitUser second = new(name: "Test User", email: "test@example.com", username: "testuser");

        Assert.Equal(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentNamesAreNotEqual()
    {
        CommitUser first = new(name: "Test User", email: "test@example.com", username: "testuser");
        CommitUser second = new(name: "Other User", email: "test@example.com", username: "testuser");

        Assert.NotEqual(expected: first, actual: second);
    }

    [Fact]
    public void TwoInstancesWithDifferentUsernamesAreNotEqual()
    {
        CommitUser first = new(name: "Test User", email: "test@example.com", username: "testuser");
        CommitUser second = new(name: "Test User", email: "test@example.com", username: null);

        Assert.NotEqual(expected: first, actual: second);
    }
}
