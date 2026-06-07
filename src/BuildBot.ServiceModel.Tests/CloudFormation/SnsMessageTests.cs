using System;
using BuildBot.ServiceModel.CloudFormation;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace BuildBot.ServiceModel.Tests.CloudFormation;

public sealed class SnsMessageTests : TestBase
{
    private static readonly DateTime Timestamp = MockDateTimeSources.Past.GetUtcNow().UtcDateTime;

    [Fact]
    public void TypePropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "Notification", actual: message.Type);
    }

    [Fact]
    public void MessageIdPropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "msg-001", actual: message.MessageId);
    }

    [Fact]
    public void TokenPropertyMatchesConstructorArgumentWhenProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "token-value", actual: message.Token);
    }

    [Fact]
    public void TokenPropertyIsNullWhenNotProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage(
            token: null,
            signingCertUrl: null,
            subscribeUrl: null,
            unsubscribeUrl: null
        );

        Assert.Null(message.Token);
    }

    [Fact]
    public void TopicArnPropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "arn:aws:sns:eu-west-1:123:test", actual: message.TopicArn);
    }

    [Fact]
    public void SubjectPropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "Test subject", actual: message.Subject);
    }

    [Fact]
    public void MessagePropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "Test message body", actual: message.Message);
    }

    [Fact]
    public void TimestampPropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: Timestamp, actual: message.Timestamp);
    }

    [Fact]
    public void SignatureVersionPropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "1", actual: message.SignatureVersion);
    }

    [Fact]
    public void SignaturePropertyMatchesConstructorArgument()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "sig==", actual: message.Signature);
    }

    [Fact]
    public void SigningCertUrlPropertyMatchesConstructorArgumentWhenProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "https://sns.amazonaws.com/cert.pem", actual: message.SigningCertUrl);
    }

    [Fact]
    public void SigningCertUrlPropertyIsNullWhenNotProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage(
            token: null,
            signingCertUrl: null,
            subscribeUrl: null,
            unsubscribeUrl: null
        );

        Assert.Null(message.SigningCertUrl);
    }

    [Fact]
    public void SubscribeUrlPropertyMatchesConstructorArgumentWhenProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "https://sns.amazonaws.com/subscribe", actual: message.SubscribeUrl);
    }

    [Fact]
    public void SubscribeUrlPropertyIsNullWhenNotProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage(
            token: null,
            signingCertUrl: null,
            subscribeUrl: null,
            unsubscribeUrl: null
        );

        Assert.Null(message.SubscribeUrl);
    }

    [Fact]
    public void UnsubscribeUrlPropertyMatchesConstructorArgumentWhenProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage();

        Assert.Equal(expected: "https://sns.amazonaws.com/unsubscribe", actual: message.UnsubscribeUrl);
    }

    [Fact]
    public void UnsubscribeUrlPropertyIsNullWhenNotProvided()
    {
        SnsMessage message = BuildDefaultSnsMessage(
            token: null,
            signingCertUrl: null,
            subscribeUrl: null,
            unsubscribeUrl: null
        );

        Assert.Null(message.UnsubscribeUrl);
    }

    private static SnsMessage BuildDefaultSnsMessage(
        string? token = "token-value",
        string? signingCertUrl = "https://sns.amazonaws.com/cert.pem",
        string? subscribeUrl = "https://sns.amazonaws.com/subscribe",
        string? unsubscribeUrl = "https://sns.amazonaws.com/unsubscribe"
    )
    {
        return new SnsMessage(
            type: "Notification",
            messageId: "msg-001",
            token: token,
            topicArn: "arn:aws:sns:eu-west-1:123:test",
            subject: "Test subject",
            message: "Test message body",
            timestamp: Timestamp,
            signatureVersion: "1",
            signature: "sig==",
            signingCertUrl: signingCertUrl,
            subscribeUrl: subscribeUrl,
            unsubscribeUrl: unsubscribeUrl
        );
    }
}
