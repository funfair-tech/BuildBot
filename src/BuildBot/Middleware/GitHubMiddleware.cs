using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
        if (!context.Request.Headers.ContainsKey(GITHUB_EVENT_HEADER))
        {
            // no github request header, pass this request on to the next middleware
            return this._next(context);
        }

        // get the event header
        string eventHeader = context.Request.Headers[GITHUB_EVENT_HEADER];

        // update the request path (this will fire corresponding methods on the github controller)
        context.Request.Path = $"/github/{eventHeader}";

        return this._next(context);
    }
}