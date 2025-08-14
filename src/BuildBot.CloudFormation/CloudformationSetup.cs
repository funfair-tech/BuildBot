using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Publishers;
using BuildBot.CloudFormation.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace BuildBot.CloudFormation;

public static class CloudformationSetup
{
    private const int MAX_RETRIES = 3;

    public static IServiceCollection AddCloudFormation(this IServiceCollection services, in SnsNotificationOptions snsConfiguration)
    {
        return services.AddSingleton(typeof(SnsNotificationOptions), implementationInstance: snsConfiguration)
                       .AddSingleton<IAwsCloudFormation, AwsCloudFormation>()
                       .AddSingleton<ICloudFormationSnsPropertiesParser, CloudFormationSnsPropertiesParser>()
                       .AddSingleton<ICloudFormationDeploymentExtractor, CloudFormationDeploymentExtractor>()
                       .AddSubscriptionConfirmationHttpClient();
    }

    private static IServiceCollection AddSubscriptionConfirmationHttpClient(this IServiceCollection services)
    {
        return services.AddHttpClient(nameof(CloudFormationSubscriptionConfirmationNotificationHandler))
                       .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(retryCount: MAX_RETRIES, sleepDurationProvider: HandleRetry, onRetryAsync: OnRetryAsync))
                       .Services;
    }

    private static Task OnRetryAsync(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, int i, Context context)
    {
        return Task.CompletedTask;
    }

    private static TimeSpan HandleRetry(int retryCount, DelegateResult<HttpResponseMessage> response, Context context)
    {
        if (response.Result is not null && response.Result.Headers.TryGetValues(name: "Retry-After", out IEnumerable<string>? result) &&
            int.TryParse(result.First(), style: NumberStyles.Integer, provider: CultureInfo.InvariantCulture, out int seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return CalculateRetryDelay(retryCount);
    }

    private static TimeSpan CalculateRetryDelay(int attempts)
    {
        // do a fast first retry, then exponential backoff
        return attempts <= 1
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(Math.Pow(x: 2, y: attempts));
    }
}