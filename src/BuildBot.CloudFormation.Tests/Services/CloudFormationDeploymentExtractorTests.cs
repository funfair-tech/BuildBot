using System;
using System.Collections.Generic;
using BuildBot.CloudFormation;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Services;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BuildBot.CloudFormation.Tests.Services;

public sealed class CloudFormationDeploymentExtractorTests : TestBase
{
    private CloudFormationDeploymentExtractor CreateExtractor()
    {
        ILogger<CloudFormationDeploymentExtractor> logger = this.GetTypedLogger<CloudFormationDeploymentExtractor>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return new CloudFormationDeploymentExtractor(logger);
    }

    private static CloudFormationMessageReceived BuildNotification(Dictionary<string, string> properties)
    {
        return new CloudFormationMessageReceived(
            TopicArn: "test-arn",
            MessageId: "msg-id",
            Properties: properties,
            Timestamp: MockDateTimeSources.Past.GetUtcNow().UtcDateTime
        );
    }

    private static Dictionary<string, string> BuildFullProperties(string resourceStatus)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ResourceType"] = "AWS::CloudFormation::Stack",
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["StackName"] = "MyStack",
            ["LogicalResourceId"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["ResourceStatus"] = resourceStatus,
        };
    }

    [Fact]
    public void ExtractDeploymentProperties_WithMissingResourceType_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["StackName"] = "MyStack",
            ["LogicalResourceId"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["ResourceStatus"] = "CREATE_COMPLETE",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithWrongResourceType_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["ResourceType"] = "SomethingElse",
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["StackName"] = "MyStack",
            ["LogicalResourceId"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["ResourceStatus"] = "CREATE_COMPLETE",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithMissingStackId_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["ResourceType"] = "AWS::CloudFormation::Stack",
            ["StackName"] = "MyStack",
            ["LogicalResourceId"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["ResourceStatus"] = "CREATE_COMPLETE",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithMissingStackName_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["ResourceType"] = "AWS::CloudFormation::Stack",
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["LogicalResourceId"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["ResourceStatus"] = "CREATE_COMPLETE",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithMissingLogicalResourceId_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["ResourceType"] = "AWS::CloudFormation::Stack",
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["StackName"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["ResourceStatus"] = "CREATE_COMPLETE",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithMissingPhysicalResourceId_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["ResourceType"] = "AWS::CloudFormation::Stack",
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["StackName"] = "MyStack",
            ["LogicalResourceId"] = "MyStack",
            ["ResourceStatus"] = "CREATE_COMPLETE",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithMissingResourceStatus_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        Dictionary<string, string> properties = new(StringComparer.Ordinal)
        {
            ["ResourceType"] = "AWS::CloudFormation::Stack",
            ["StackId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            ["StackName"] = "MyStack",
            ["LogicalResourceId"] = "MyStack",
            ["PhysicalResourceId"] = "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
        };

        CloudFormationMessageReceived notification = BuildNotification(properties);

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithUnknownResourceStatus_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        CloudFormationMessageReceived notification = BuildNotification(BuildFullProperties("UNKNOWN_STATUS"));

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithNullSuccessStatus_ReturnsNull()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        CloudFormationMessageReceived notification = BuildNotification(BuildFullProperties("UPDATE_IN_PROGRESS"));

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("UPDATE_IN_PROGRESS")]
    [InlineData("UPDATE_COMPLETE_CLEANUP_IN_PROGRESS")]
    public void ExtractDeploymentProperties_WithNullSuccessStatuses_ReturnsNull(string status)
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        CloudFormationMessageReceived notification = BuildNotification(BuildFullProperties(status));

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithSuccessStatus_ReturnsDeploymentWithSuccessTrue()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        CloudFormationMessageReceived notification = BuildNotification(BuildFullProperties("CREATE_COMPLETE"));

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.NotNull(result);
        Assert.True(result.Value.Success, userMessage: "Expected Success to be true");
        Assert.Equal(expected: "MyStack", actual: result.Value.StackName);
    }

    [Fact]
    public void ExtractDeploymentProperties_WithFailureStatus_ReturnsDeploymentWithSuccessFalse()
    {
        CloudFormationDeploymentExtractor extractor = this.CreateExtractor();

        CloudFormationMessageReceived notification = BuildNotification(BuildFullProperties("CREATE_FAILED"));

        Deployment? result = extractor.ExtractDeploymentProperties(in notification);

        Assert.NotNull(result);
        Assert.False(result.Value.Success, userMessage: "Expected Success to be false");
    }
}
