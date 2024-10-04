using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                      handler: static async ([FromBody] string body, ILogger<RouteGroupBuilder> logger, CancellationToken cancellationToken) =>
                               {
                                   logger.LogError(LogMessage(body));

                                   await Task.Delay(millisecondsDelay: 1, cancellationToken: cancellationToken);

                                   return Results.NoContent();
                               })
             .Accepts<string>(MediaTypeNames.Text.Plain);

        return app;
    }

    private static string LogMessage(string body)
    {
        return "CLOUDFORMATION: >>>>" + body + "<<<<";
    }
}