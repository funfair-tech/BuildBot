using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureWatchtowerEndpoints(this WebApplication app)
    {
        Console.WriteLine("Configuring Cloudformation Endpoint");

        RouteGroupBuilder group = app.MapGroup("/watchtower");
        group.MapPost(pattern: "deploy",
                      handler: static async (HttpRequest request, ILogger<WebApplication> logger, CancellationToken cancellationToken) =>
                               {
                                   ReadResult result = await request.BodyReader.ReadAsync(cancellationToken: cancellationToken);

                                   string text = Encoding.UTF8.GetString(result.Buffer);
                                   logger.LogError(text);
                               });

        return app;
    }

    // private static async ValueTask<IResult> HandleDeployMessageAsync(
    //     SnsMessage message,
    //     ICloudFormationSnsPropertiesParser parser,
    //     IMediator mediator,
    //     CancellationToken cancellationToken
    // )
    // {
    //     if (StringComparer.Ordinal.Equals(x: message.Type, y: "SubscriptionConfirmation"))
    //     {
    //         return await HandleSnsSubscriptionMessagesAsync(
    //             message: message,
    //             mediator: mediator,
    //             cancellationToken: cancellationToken
    //         );
    //     }
    //
    //     if (StringComparer.Ordinal.Equals(x: message.Type, y: "Notification"))
    //     {
    //         return await HandleNotiifcationMessageAsync(
    //             message: message,
    //             parser: parser,
    //             mediator: mediator,
    //             cancellationToken: cancellationToken
    //         );
    //     }
    //
    //     return Results.NotFound();
    // }

    // private static async ValueTask<IResult> HandleNotiifcationMessageAsync(
    //     SnsMessage message,
    //     ICloudFormationSnsPropertiesParser parser,
    //     IMediator mediator,
    //     CancellationToken cancellationToken
    // )
    // {
    //     Dictionary<string, string> properties = parser.SplitMessageToDictionary(message);
    //
    //     await mediator.Publish(
    //         new CloudFormationMessageReceived(
    //             TopicArn: message.TopicArn,
    //             MessageId: message.MessageId,
    //             Properties: properties,
    //             Timestamp: message.Timestamp
    //         ),
    //         cancellationToken: cancellationToken
    //     );
    //
    //     return Results.NoContent();
    // }

    // private static async ValueTask<IResult> HandleSnsSubscriptionMessagesAsync(
    //     SnsMessage message,
    //     IMediator mediator,
    //     CancellationToken cancellationToken
    // )
    // {
    //     if (message.SubscribeUrl is null)
    //     {
    //         return Results.BadRequest();
    //     }
    //
    //     await mediator.Publish(
    //         new CloudFormationSubscriptionConfirmation(TopicArn: message.TopicArn, new(message.SubscribeUrl)),
    //         cancellationToken: cancellationToken
    //     );
    //
    //     return Results.Accepted();
    // }

    // private static async ValueTask<T> ReadJsonAsync<T>(HttpRequest request, CancellationToken cancellationToken)
    // {
    //     if (AppSerializationContext.Default.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> typeInfo)
    //     {
    //         return NoTypeInformation<T>();
    //     }
    //
    //     T? result = await JsonSerializer.DeserializeAsync(
    //         utf8Json: request.Body,
    //         jsonTypeInfo: typeInfo,
    //         cancellationToken: cancellationToken
    //     );
    //
    //     return result ?? InvalidJson<T>();
    // }

    // [DoesNotReturn]
    // private static T NoTypeInformation<T>()
    // {
    //     throw new JsonException("No type handler found");
    // }
    //
    // [DoesNotReturn]
    // private static T InvalidJson<T>()
    // {
    //     throw new JsonException("Could not parse JSON");
    // }
}