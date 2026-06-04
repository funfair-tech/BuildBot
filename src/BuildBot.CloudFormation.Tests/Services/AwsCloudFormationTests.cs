using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using BuildBot.CloudFormation;
using BuildBot.CloudFormation.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using SystemInvalidOperationException = System.InvalidOperationException;

namespace BuildBot.CloudFormation.Tests.Services;

public sealed class AwsCloudFormationTests : TestBase
{
    private (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) CreateHandler()
    {
        IAmazonCloudFormation cloudFormationClient = GetSubstitute<IAmazonCloudFormation>();
        ILogger<AwsCloudFormation> logger = this.GetTypedLogger<AwsCloudFormation>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return (
            cloudFormationClient,
            new AwsCloudFormation(cloudFormationClient: cloudFormationClient, logger: logger)
        );
    }

    private static Deployment MakeDeployment()
    {
        return new Deployment(
            StackName: "MyStack",
            Project: "MyStack",
            Arn: "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc",
            Status: "CREATE_COMPLETE",
            Success: true,
            StackId: "arn:aws:cloudformation:eu-west-1:123:stack/MyStack/abc"
        );
    }

    [Fact]
    public async Task GetStackDetailsAsync_WhenDescribeReturnsEmptyStacks_ReturnsNullAsync()
    {
        (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) = this.CreateHandler();

        cloudFormationClient
            .DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DescribeStacksResponse { Stacks = [] }));

        StackDetails? result = await handler.GetStackDetailsAsync(
            deployment: MakeDeployment(),
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStackDetailsAsync_WhenStackHasNoDescriptionOrVersion_ReturnsNullAsync()
    {
        (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) = this.CreateHandler();

        cloudFormationClient
            .DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new DescribeStacksResponse { Stacks = [new Stack { Description = string.Empty, Tags = [] }] }
                )
            );

        StackDetails? result = await handler.GetStackDetailsAsync(
            deployment: MakeDeployment(),
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStackDetailsAsync_WhenStackHasDescription_ReturnsStackDetailsAsync()
    {
        (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) = this.CreateHandler();

        cloudFormationClient
            .DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new DescribeStacksResponse { Stacks = [new Stack { Description = "My stack", Tags = [] }] }
                )
            );

        StackDetails? result = await handler.GetStackDetailsAsync(
            deployment: MakeDeployment(),
            cancellationToken: this.CancellationToken()
        );

        Assert.NotNull(result);
        Assert.Equal(expected: "My stack", actual: result.Value.Description);
    }

    [Fact]
    public async Task GetStackDetailsAsync_WhenStackHasVersionTag_ReturnsStackDetailsAsync()
    {
        (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) = this.CreateHandler();

        cloudFormationClient
            .DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new DescribeStacksResponse
                    {
                        Stacks =
                        [
                            new Stack
                            {
                                Description = string.Empty,
                                Tags = [new Tag { Key = "Version", Value = "1.0.0" }],
                            },
                        ],
                    }
                )
            );

        StackDetails? result = await handler.GetStackDetailsAsync(
            deployment: MakeDeployment(),
            cancellationToken: this.CancellationToken()
        );

        Assert.NotNull(result);
        Assert.Equal(expected: "1.0.0", actual: result.Value.Version);
    }

    [Fact]
    public async Task GetStackDetailsAsync_WhenStackHasDescriptionAndVersion_ReturnsStackDetailsAsync()
    {
        (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) = this.CreateHandler();

        cloudFormationClient
            .DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new DescribeStacksResponse
                    {
                        Stacks =
                        [
                            new Stack
                            {
                                Description = "My stack",
                                Tags = [new Tag { Key = "Version", Value = "1.0.0" }],
                            },
                        ],
                    }
                )
            );

        StackDetails? result = await handler.GetStackDetailsAsync(
            deployment: MakeDeployment(),
            cancellationToken: this.CancellationToken()
        );

        Assert.NotNull(result);
        Assert.Equal(expected: "My stack", actual: result.Value.Description);
        Assert.Equal(expected: "1.0.0", actual: result.Value.Version);
    }

    [Fact]
    public async Task GetStackDetailsAsync_WhenExceptionThrown_ReturnsNullAsync()
    {
        (IAmazonCloudFormation cloudFormationClient, AwsCloudFormation handler) = this.CreateHandler();

        cloudFormationClient
            .DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DescribeStacksResponse>(new SystemInvalidOperationException("error")));

        StackDetails? result = await handler.GetStackDetailsAsync(
            deployment: MakeDeployment(),
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }
}
