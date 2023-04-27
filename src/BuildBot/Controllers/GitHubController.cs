using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.GitHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers;

public sealed class GitHubController : BuildBotControllerBase
{
    private readonly IPublisher<Push> _pushPublisher;
    private readonly IPublisher<Status> _statusPublisher;

    public GitHubController(IPublisher<Push> pushPublisher, IPublisher<Status> statusPublisher, ILogger<GitHubController> logger)
        : base(logger: logger)
    {
        this._pushPublisher = pushPublisher;
        this._statusPublisher = statusPublisher;
    }

    [HttpPost]
    [Route(template: "ping")]
    public IActionResult Ping([FromBody] Ping request)
    {
        this.Logger.LogTrace($"Ping: {request.HookId}");

        return this.NoContent();
    }

    [HttpPost]
    [Route(template: "push")]
    public Task<IActionResult> PushAsync([FromBody] Push request, CancellationToken cancellationToken)
    {
        return this.ProcessAsync(action: () => this._pushPublisher.PublishAsync(message: request, cancellationToken: cancellationToken));
    }

    [HttpPost]
    [Route(template: "status")]
    public Task<IActionResult> StatusAsync([FromBody] Status request, CancellationToken cancellationToken)
    {
        return this.ProcessAsync(action: () => this._statusPublisher.PublishAsync(message: request, cancellationToken: cancellationToken));
    }
}