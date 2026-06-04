using BuildBot.CloudFormation.Configuration;
using FunFair.Test.Common;
using Xunit;

namespace BuildBot.CloudFormation.Tests.Configuration;

public sealed class SnsNotificationOptionsTests : TestBase
{
    [Fact]
    public void IsValidArn_WithMatchingArn_ReturnsTrue()
    {
        SnsNotificationOptions options = new(
            TopicArn: "arn:aws:sns:eu-west-1:123:test",
            Region: "eu-west-1",
            AccessKey: "AKIAIOSFODNN7EXAMPLE",
            SecretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        );

        bool result = options.IsValidArn("arn:aws:sns:eu-west-1:123:test");

        Assert.True(result, userMessage: "ARN should match");
    }

    [Fact]
    public void IsValidArn_WithNonMatchingArn_ReturnsFalse()
    {
        SnsNotificationOptions options = new(
            TopicArn: "arn:aws:sns:eu-west-1:123:test",
            Region: "eu-west-1",
            AccessKey: "AKIAIOSFODNN7EXAMPLE",
            SecretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        );

        bool result = options.IsValidArn("arn:aws:sns:eu-west-1:123:other");

        Assert.False(result, userMessage: "ARN should not match");
    }
}
