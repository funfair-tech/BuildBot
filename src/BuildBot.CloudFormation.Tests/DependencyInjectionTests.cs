using BuildBot.CloudFormation;
using BuildBot.CloudFormation.Configuration;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.CloudFormation.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure) { }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddCloudFormation(
            new SnsNotificationOptions(
                TopicArn: "arn:aws:sns:eu-west-1:123456789012:test-topic",
                Region: "eu-west-1",
                AccessKey: "AKIAIOSFODNN7EXAMPLE",
                SecretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
            )
        );
    }

    [Fact]
    public void AwsCloudFormationMustBeRegistered()
    {
        this.RequireService<IAwsCloudFormation>();
    }

    [Fact]
    public void CloudFormationSnsPropertiesParserMustBeRegistered()
    {
        this.RequireService<ICloudFormationSnsPropertiesParser>();
    }

    [Fact]
    public void CloudFormationDeploymentExtractorMustBeRegistered()
    {
        this.RequireService<ICloudFormationDeploymentExtractor>();
    }
}
