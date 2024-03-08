using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    private static WebApplication ConfigureTestEndpoints(this WebApplication app)
    {
        app.MapGet(pattern: "/ping", handler: () => Results.Ok(PingPong.Model));

        return app;
    }
}