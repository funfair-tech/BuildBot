﻿using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.GitHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers;

[Route(template: "[controller]")]
public sealed class GitHubController : ControllerBase
{
    private readonly ILogger<GitHubController> _logger;
    private readonly IPublisher<Push> _pushPublisher;
    private readonly IPublisher<Status> _statusPublisher;

    public GitHubController(IPublisher<Push> pushPublisher, IPublisher<Status> statusPublisher, ILogger<GitHubController> logger)
    {
        this._pushPublisher = pushPublisher;
        this._statusPublisher = statusPublisher;
        this._logger = logger;
    }

    private async Task<IActionResult> ProcessAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, message: exception.Message);
        }

        return this.NoContent();
    }

    [HttpPost]
    [Route(template: "ping")]
    public IActionResult Ping([FromBody] Ping request)
    {
        this._logger.LogTrace($"Ping: {request.HookId}");

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