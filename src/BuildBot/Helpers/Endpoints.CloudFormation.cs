using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Models;
using BuildBot.Json;
using BuildBot.ServiceModel.CloudFormation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureCloudformationEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Cloudformation Endpoint");

        RouteGroupBuilder group = app.MapGroup("/cloudformation");
        group.MapPost(pattern: "deploy",
                      handler: static async (HttpRequest request, IMediator mediator, ILogger<RouteGroupBuilder> logger, CancellationToken cancellationToken) =>
                               {
                                   SnsMessage message = await ReadJsonAsync<SnsMessage>(request: request, cancellationToken: cancellationToken);
                                   logger.LogError(LogMessage(message));

                                   return await HandleDeployMessageAsync(message: message, mediator: mediator, cancellationToken: cancellationToken);
                               })
             .Accepts<SnsMessage>(contentType: "text/plain", "application/json");

        return app;
    }

    private static async ValueTask<IResult> HandleDeployMessageAsync(SnsMessage message, IMediator mediator, CancellationToken cancellationToken)
    {
        if (StringComparer.Ordinal.Equals(x: message.Type, y: "SubscriptionConfirmation"))
        {
            if (message.SubscribeUrl is not null)
            {
                await mediator.Publish(new CloudFormationSubscriptionConfirmation(TopicArn: message.TopicArn, new(message.SubscribeUrl)), cancellationToken: cancellationToken);

                return Results.Accepted();
            }

            return Results.BadRequest();
        }

        if (StringComparer.Ordinal.Equals(x: message.Type, y: "Notification"))
        {
            Dictionary<string, string> properties = SplitMessageToDictionary(message);

            await mediator.Publish(new CloudFormationMessageReceived(TopicArn: message.TopicArn, MessageId: message.MessageId, Properties: properties, Timestamp: message.Timestamp),
                                   cancellationToken: cancellationToken);

            return Results.NoContent();
        }

        return Results.NotFound();
    }

    private static Dictionary<string, string> SplitMessageToDictionary(SnsMessage message)
    {
        return message.Message.Split("\n")
                      .Where(line => !string.IsNullOrWhiteSpace(line))
                      .Select(SplitLineToKeyAndValue)
                      .ToDictionary(keySelector: key => key.key, elementSelector: value => value.value, comparer: StringComparer.Ordinal);
    }

    private static (string key, string value) SplitLineToKeyAndValue(string m)
    {
        const string resourceValueStart = "='";
        const string resourceValueEnd = "'";

        int i = m.IndexOf(value: resourceValueStart, comparisonType: StringComparison.Ordinal);

        if (i < resourceValueStart.Length)
        {
            return (key: m, string.Empty);
        }

        string key = m.Substring(startIndex: 0, length: i);
        int start = i + resourceValueStart.Length;
        int length = m.Length - start - resourceValueEnd.Length;

        string value = m.Substring(startIndex: start, length: length);

        return (key, value);
    }

    private static async ValueTask<T> ReadJsonAsync<T>(HttpRequest request, CancellationToken cancellationToken)
    {
        if (AppSerializationContext.Default.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> typeInfo)
        {
            throw new JsonException("No type handler found");
        }

        T? result = await JsonSerializer.DeserializeAsync(utf8Json: request.Body, jsonTypeInfo: typeInfo, cancellationToken: cancellationToken);

        if (result is not null)
        {
            return result;
        }

        throw new JsonException("Could not parse JSON");
    }

    private static string LogMessage(SnsMessage body)
    {
        return AppSerializationContext.Default.GetTypeInfo(typeof(SnsMessage)) is JsonTypeInfo<SnsMessage> typeInfo
            ? JsonSerializer.Serialize(value: body, jsonTypeInfo: typeInfo)
            : "CLOUDFORMATION: >>>>" + body.Type + "<<<<" + body.Message;
    }
}