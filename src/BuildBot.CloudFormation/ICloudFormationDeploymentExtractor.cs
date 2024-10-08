using BuildBot.CloudFormation.Models;

namespace BuildBot.CloudFormation;

public interface ICloudFormationDeploymentExtractor
{
    Deployment? ExtractDeploymentProperties(in CloudFormationMessageReceived notification);
}