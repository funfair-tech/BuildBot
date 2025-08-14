using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using BuildBot.CloudFormation.Publishers.LoggingExtensions;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationSubscriptionConfirmationNotificationHandler
    : INotificationHandler<CloudFormationSubscriptionConfirmation>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> _logger;
    private readonly SnsNotificationOptions _options;

    [SuppressMessage(
        category: "Roslynator.Analyzers",
        checkId: "RCS1231: Make parameter ref read-only.",
        Justification = "DI"
    )]
    public CloudFormationSubscriptionConfirmationNotificationHandler(
        IHttpClientFactory httpClientFactory,
        SnsNotificationOptions options,
        ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger
    )
    {
        this._httpClientFactory = httpClientFactory;
        this._options = options;
        this._logger = logger;
    }

    [SuppressMessage(
        category: "",
        checkId: "CSE007: Handle dispose correctly.",
        Justification = "HttpClient is managed by the HttpClientFactory."
    )]
    public async ValueTask Handle(
        CloudFormationSubscriptionConfirmation notification,
        CancellationToken cancellationToken
    )
    {
        if (!this._options.IsValidArn(notification.TopicArn))
        {
            this._logger.ReceivedInvalidArn(notification.TopicArn);

            return;
        }

        try
        {
            HttpClient client = this._httpClientFactory.CreateClient(
                nameof(CloudFormationSubscriptionConfirmationNotificationHandler)
            );

            HttpResponseMessage responseMessage = await client.GetAsync(
                requestUri: notification.SubscribeUrl,
                cancellationToken: cancellationToken
            );
            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception exception)
        {
            this._logger.FailedToSubscribeToTopic(
                subscribeUrl: notification.SubscribeUrl,
                message: exception.Message,
                exception: exception
            );
        }
    }
}
