using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.CloudFormation.Models;
using BuildBot.ServiceModel.CloudFormation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BuildBot.CloudFormation;

public static class CloudFormationEndpoints
{
    public static WebApplication MapCloudFormationEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Cloudformation Endpoint");

        RouteGroupBuilder group = app.MapGroup("/cloudformation");

        // SNS posts with a text/plain content type, so the body is read directly rather than model bound.
        group
            .MapPost(pattern: "deploy", handler: DeployAsync)
            .Accepts<SnsMessage>(contentType: "text/plain", "application/json");

        return app;
    }

    public static async Task<IResult> DeployAsync(
        HttpRequest request,
        ICloudFormationSnsPropertiesParser parser,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        SnsMessage message = await ReadSnsMessageAsync(request: request, cancellationToken: cancellationToken);

        return await HandleDeployMessageAsync(
            message: message,
            parser: parser,
            mediator: mediator,
            cancellationToken: cancellationToken
        );
    }

    public static async Task<IResult> HandleDeployMessageAsync(
        SnsMessage message,
        ICloudFormationSnsPropertiesParser parser,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        if (StringComparer.Ordinal.Equals(x: message.Type, y: "SubscriptionConfirmation"))
        {
            return await HandleSnsSubscriptionMessageAsync(
                message: message,
                mediator: mediator,
                cancellationToken: cancellationToken
            );
        }

        if (StringComparer.Ordinal.Equals(x: message.Type, y: "Notification"))
        {
            return await HandleNotificationMessageAsync(
                message: message,
                parser: parser,
                mediator: mediator,
                cancellationToken: cancellationToken
            );
        }

        return Results.NotFound();
    }

    private static async ValueTask<IResult> HandleNotificationMessageAsync(
        SnsMessage message,
        ICloudFormationSnsPropertiesParser parser,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, string> properties = parser.SplitMessageToDictionary(message);

        await mediator.Publish(
            new CloudFormationMessageReceived(
                TopicArn: message.TopicArn,
                MessageId: message.MessageId,
                Properties: properties,
                Timestamp: message.Timestamp
            ),
            cancellationToken: cancellationToken
        );

        return Results.NoContent();
    }

    private static async ValueTask<IResult> HandleSnsSubscriptionMessageAsync(
        SnsMessage message,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        if (message.SubscribeUrl is null)
        {
            return Results.BadRequest();
        }

        await mediator.Publish(
            new CloudFormationSubscriptionConfirmation(TopicArn: message.TopicArn, new(message.SubscribeUrl)),
            cancellationToken: cancellationToken
        );

        return Results.Accepted();
    }

    private static async ValueTask<SnsMessage> ReadSnsMessageAsync(
        HttpRequest request,
        CancellationToken cancellationToken
    )
    {
        SnsMessage? message = await JsonSerializer.DeserializeAsync(
            utf8Json: request.Body,
            jsonTypeInfo: CloudFormationSerializationContext.Default.SnsMessage,
            cancellationToken: cancellationToken
        );

        return message ?? InvalidJson();
    }

    [DoesNotReturn]
    private static SnsMessage InvalidJson()
    {
        throw new JsonException("Could not parse JSON");
    }
}
