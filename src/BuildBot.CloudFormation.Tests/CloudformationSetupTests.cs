using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Polly;
using Xunit;

namespace BuildBot.CloudFormation.Tests;

public sealed class CloudformationSetupTests : TestBase
{
    [Fact]
    public async Task OnRetryAsync_ReturnsCompletedTask()
    {
        using HttpResponseMessage response = new(HttpStatusCode.InternalServerError);
        DelegateResult<HttpResponseMessage> delegateResult = new(response);
        Context context = [];

        Task result = CloudformationSetup.OnRetryAsync(
            delegateResult: delegateResult,
            timeSpan: TimeSpan.Zero,
            i: 1,
            context: context
        );

        await result;

        Assert.True(result.IsCompletedSuccessfully, userMessage: "Expected task to complete successfully");
    }

    [Fact]
    public void CalculateRetryDelay_WithFirstAttempt_ReturnsZero()
    {
        TimeSpan result = CloudformationSetup.CalculateRetryDelay(attempts: 1);

        Assert.Equal(expected: TimeSpan.Zero, actual: result);
    }

    [Fact]
    public void CalculateRetryDelay_WithZeroAttempts_ReturnsZero()
    {
        TimeSpan result = CloudformationSetup.CalculateRetryDelay(attempts: 0);

        Assert.Equal(expected: TimeSpan.Zero, actual: result);
    }

    [Fact]
    public void CalculateRetryDelay_WithSecondAttempt_ReturnsExponentialDelay()
    {
        TimeSpan result = CloudformationSetup.CalculateRetryDelay(attempts: 2);

        Assert.Equal(expected: TimeSpan.FromSeconds(4), actual: result);
    }

    [Fact]
    public void HandleRetry_WithRetryAfterHeader_ReturnsRetryAfterDuration()
    {
        using HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
        response.Headers.Add("Retry-After", "30");
        DelegateResult<HttpResponseMessage> delegateResult = new(response);
        Context context = [];

        TimeSpan result = CloudformationSetup.HandleRetry(retryCount: 1, response: delegateResult, context: context);

        Assert.Equal(expected: TimeSpan.FromSeconds(30), actual: result);
    }

    [Fact]
    public void HandleRetry_WithoutRetryAfterHeader_UsesCalculatedDelay()
    {
        using HttpResponseMessage response = new(HttpStatusCode.InternalServerError);
        DelegateResult<HttpResponseMessage> delegateResult = new(response);
        Context context = [];

        TimeSpan result = CloudformationSetup.HandleRetry(retryCount: 1, response: delegateResult, context: context);

        Assert.Equal(expected: TimeSpan.Zero, actual: result);
    }

    [Fact]
    public void HandleRetry_WithException_UsesCalculatedDelay()
    {
        DelegateResult<HttpResponseMessage> delegateResult = new(new InvalidOperationException("error"));
        Context context = [];

        TimeSpan result = CloudformationSetup.HandleRetry(retryCount: 2, response: delegateResult, context: context);

        Assert.Equal(expected: TimeSpan.FromSeconds(4), actual: result);
    }
}
