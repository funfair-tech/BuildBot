using Microsoft.AspNetCore.Builder;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        return app.ConfigureTestEndpoints()
                  .ConfigureCloudformationEndpoints()
                  .ConfigureGitHubEndpoints()
                  .ConfigureWatchtowerEndpoints();
    }
}