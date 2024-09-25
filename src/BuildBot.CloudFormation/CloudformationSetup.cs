using Microsoft.Extensions.DependencyInjection;

namespace BuildBot.CloudFormation;

public static class CloudformationSetup
{
    public static IServiceCollection AddCloudFormation(this IServiceCollection services)
    {
        return services;
    }
}