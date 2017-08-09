using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BuildBot.Middleware
{
    public class GitHubMiddleware
    {
        private readonly RequestDelegate _next;
        private const string GITHUB_EVENT_HEADER = "X-GitHub-Event";

        public GitHubMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey(GITHUB_EVENT_HEADER))
            {
                // no github request header, pass this request on to the next middleware
                return this._next.Invoke(context);
            }

            // get the event header
            string eventHeader = context.Request.Headers[GITHUB_EVENT_HEADER];

            // update the request path (this will fire corresponding methods on the github controller)
            context.Request.Path = $"/github/{eventHeader}";

            return this._next.Invoke(context);
        }
    }
}
