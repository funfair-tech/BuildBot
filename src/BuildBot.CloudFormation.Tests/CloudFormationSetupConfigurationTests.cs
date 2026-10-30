using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildBot.CloudFormation.Tests;

public sealed class CloudFormationSetupConfigurationTests : TestBase
{
    private const string TOPIC_ARN_KEY = "CloudFormation:TopicArn";
    private const string REGION_KEY = "CloudFormation:Aws:Region";
    private const string ACCESS_KEY_ID_KEY = "CloudFormation:Aws:AccessKeyId";
    private const string SECRET_KEY_KEY = "CloudFormation:Aws:SecretKey";

    private const string TOPIC_ARN = "arn:aws:sns:eu-west-1:123456789012:test-topic";
    private const string REGION = "eu-west-1";
    private const string ACCESS_KEY = "AKIAIOSFODNN7EXAMPLE";
    private const string SECRET_KEY = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

    [Fact]
    public async Task PresentSettingsAreBoundAsync()
    {
        SnsNotificationOptions options = await ResolveOptionsAsync(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [TOPIC_ARN_KEY] = TOPIC_ARN,
                [REGION_KEY] = REGION,
                [ACCESS_KEY_ID_KEY] = ACCESS_KEY,
                [SECRET_KEY_KEY] = SECRET_KEY,
            }
        );

        Assert.Equal(expected: TOPIC_ARN, actual: options.TopicArn);
        Assert.Equal(expected: REGION, actual: options.Region);
        Assert.Equal(expected: ACCESS_KEY, actual: options.AccessKey);
        Assert.Equal(expected: SECRET_KEY, actual: options.SecretKey);
    }

    [Fact]
    public async Task MissingSettingsBindToEmptyAsync()
    {
        SnsNotificationOptions options = await ResolveOptionsAsync(
            new Dictionary<string, string?>(StringComparer.Ordinal) { [REGION_KEY] = REGION }
        );

        Assert.Equal(expected: string.Empty, actual: options.TopicArn);
        Assert.Equal(expected: REGION, actual: options.Region);
        Assert.Equal(expected: string.Empty, actual: options.AccessKey);
        Assert.Equal(expected: string.Empty, actual: options.SecretKey);
    }

    [Fact]
    public async Task MissingRegionBindsToEmptyAsync()
    {
        SnsNotificationOptions options = await ResolveOptionsAsync(
            new Dictionary<string, string?>(StringComparer.Ordinal)
        );

        Assert.Equal(expected: string.Empty, actual: options.TopicArn);
        Assert.Equal(expected: string.Empty, actual: options.Region);
        Assert.Equal(expected: string.Empty, actual: options.AccessKey);
        Assert.Equal(expected: string.Empty, actual: options.SecretKey);
    }

    private static async ValueTask<SnsNotificationOptions> ResolveOptionsAsync(
        IReadOnlyDictionary<string, string?> settings
    )
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        await using ServiceProvider serviceProvider = new ServiceCollection()
            .AddMockedService<IMediator>()
            .AddCloudFormation(configuration)
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<SnsNotificationOptions>();
    }
}
