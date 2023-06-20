using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace BuildBot.Middleware;

public sealed class GitHubMiddleware
{
    private const string GITHUB_EVENT_HEADER = "X-GitHub-Event";
    private readonly RequestDelegate _next;

    public GitHubMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(key: GITHUB_EVENT_HEADER, out StringValues eventHeader))
        {
            // no github request header, pass this request on to the next middleware
            return this._next(context);
        }

        // update the request path (this will fire corresponding methods on the github controller)
        context.Request.Path = $"/github/{eventHeader}";

        return this._next(context);
    }
}