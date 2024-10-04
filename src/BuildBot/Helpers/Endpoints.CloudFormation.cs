using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.ServiceModel.CloudFormation;
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
                      handler: static async (SnsMessage message, ILogger<RouteGroupBuilder> logger, CancellationToken cancellationToken) =>
                               {
                                   logger.LogError(LogMessage(message));

                                   await Task.Delay(millisecondsDelay: 1, cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               })
             .Accepts<string>(contentType: "text/plain", "application/json");

        return app;
    }

    private static string LogMessage(SnsMessage body)
    {
        return "CLOUDFORMATION: >>>>" + body.Type + "<<<<" + body.Message;
    }
}