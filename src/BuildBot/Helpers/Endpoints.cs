using BuildBot.CloudFormation;
using BuildBot.GitHub;
using Microsoft.AspNetCore.Builder;

namespace BuildBot.Helpers;

internal static partial class Endpoints
{
    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        return app.ConfigureTestEndpoints()
            .MapCloudFormationEndpoints()
            .MapGitHubEndpoints()
            .ConfigureWatchtowerEndpoints();
    }
}
