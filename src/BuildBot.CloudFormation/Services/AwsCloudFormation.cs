using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using BuildBot.CloudFormation.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Services;

public sealed class AwsCloudFormation : IAwsCloudFormation
{
    private readonly IAmazonCloudFormation _cloudFormationClient;
    private readonly ILogger<AwsCloudFormation> _logger;

    public AwsCloudFormation(IAmazonCloudFormation cloudFormationClient, ILogger<AwsCloudFormation> logger)
    {
        this._cloudFormationClient = cloudFormationClient;
        this._logger = logger;
    }

    public async ValueTask<StackDetails?> GetStackDetailsAsync(
        Deployment deployment,
        CancellationToken cancellationToken
    )
    {
        try
        {
            DescribeStacksRequest request = new() { StackName = deployment.StackName };

            DescribeStacksResponse result = await this._cloudFormationClient.DescribeStacksAsync(
                request: request,
                cancellationToken: cancellationToken
            );

            Stack? stack = result.Stacks.FirstOrDefault();

            if (stack is null)
            {
                return null;
            }

            string projectDescription = stack.Description;

            Tag? versionTag = stack.Tags.Find(t => StringComparer.Ordinal.Equals(x: t.Key, y: "Version"));

            string? projectVersion = versionTag?.Value;

            if (!string.IsNullOrWhiteSpace(projectDescription) || !string.IsNullOrWhiteSpace(projectVersion))
            {
                return new StackDetails(Description: projectDescription, Version: projectVersion);
            }

            return null;
        }
        catch (Exception exception)
        {
            this._logger.FailedToGetCloudFormationStack(message: exception.Message, exception: exception);

            return null;
        }
    }
}
