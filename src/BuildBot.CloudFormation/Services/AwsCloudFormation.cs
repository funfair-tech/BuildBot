using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Services;

public sealed class AwsCloudFormation : IAwsCloudFormation
{
    private readonly BasicAWSCredentials _credentials;
    private readonly RegionEndpoint _endpoint;
    private readonly ILogger<AwsCloudFormation> _logger;
    private readonly SnsNotificationOptions _options;

    public AwsCloudFormation(SnsNotificationOptions options, ILogger<AwsCloudFormation> logger)
    {
        this._options = options;
        this._logger = logger;

        this._endpoint = RegionEndpoint.GetBySystemName(this._options.Region);
        this._credentials = new(accessKey: this._options.AccessKey, secretKey: this._options.SecretKey);
    }

    public async ValueTask<StackDetails?> GetStackDetailsAsync(Deployment deployment, CancellationToken cancellationToken)
    {
        try
        {
            using (AmazonCloudFormationClient cloudFormationClient = new(credentials: this._credentials, region: this._endpoint))
            {
                DescribeStacksRequest request = new() { StackName = deployment.StackName };

                DescribeStacksResponse? result = await cloudFormationClient.DescribeStacksAsync(request: request, cancellationToken: cancellationToken);

                if (result is null)
                {
                    return null;
                }

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
        }
        catch (Exception exception)
        {
            this._logger.FailedToGetCloudFormationStack(message: exception.Message, exception: exception);

            return null;
        }
    }
}
