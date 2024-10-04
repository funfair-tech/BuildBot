using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Configuration;
using BuildBot.CloudFormation.Models;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BuildBot.CloudFormation.Publishers;

public sealed class CloudFormationSubscriptionConfirmationNotificationHandler : INotificationHandler<CloudFormationSubscriptionConfirmation>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> _logger;
    private readonly SnsNotificationOptions _options;

    // https://sns.eu-west-1.amazonaws.com/?Action=ConfirmSubscription&TopicArn=arn:aws:sns:eu-west-1:117769150821:BuildBotDeployments&Token=2336412f37fb687f5d51e6e2425ba1f30ada00b04c0688e251ce22251d1c806ac0afbc4e8033d220dc8b3262f58535e6b029b8adbf2e0ff2238de9c10cd3c2595f210079fe4a127f7d35cec5b3a384a02027a253631106cd138875ec9fed674dc8d2cdc4655c2840d8b5160b814e86e5d065910a637b291e3a42a4f92bae4f1d

    public CloudFormationSubscriptionConfirmationNotificationHandler(IHttpClientFactory httpClientFactory,
                                                                     SnsNotificationOptions options,
                                                                     ILogger<CloudFormationSubscriptionConfirmationNotificationHandler> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._options = options;
        this._logger = logger;
    }

    [SuppressMessage(category: "", checkId: "CSE007: Handle dispose correctly.", Justification = "HttpClient is managed by the HttpClientFactory.")]
    public async ValueTask Handle(CloudFormationSubscriptionConfirmation notification, CancellationToken cancellationToken)
    {
        if (!this.IsValidArn(notification.TopicArn))
        {
            this._logger.LogError(message: "Invalid TopicArn: {TopicArn}", notification.TopicArn);

            return;
        }

        HttpClient client = this._httpClientFactory.CreateClient(nameof(CloudFormationSubscriptionConfirmationNotificationHandler));

        HttpResponseMessage responseMessage = await client.GetAsync(requestUri: notification.SubscribeUrl, cancellationToken: cancellationToken);
        responseMessage.EnsureSuccessStatusCode();
    }

    private bool IsValidArn(string notificationTopicArn)
    {
        return StringComparer.Ordinal.Equals(x: notificationTopicArn, y: this._options.TopicArn);
    }
}