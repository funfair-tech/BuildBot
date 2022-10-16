using System;
using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.Octopus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers;

[Route(template: "[controller]")]
public sealed class OctopusController : ControllerBase
{
    private readonly IPublisher<Deploy> _deployPublisher;
    private readonly ILogger<OctopusController> _logger;

    public OctopusController(IPublisher<Deploy> deployPublisher, ILogger<OctopusController> logger)
    {
        this._deployPublisher = deployPublisher;
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

        return this.Ok();
    }

    [HttpPost]
    [Route(template: "deploy")]
    public Task<IActionResult> PushAsync([FromBody] Deploy request, CancellationToken cancellationToken)
    {
        return this.ProcessAsync(action: () => this._deployPublisher.PublishAsync(message: request, cancellationToken: cancellationToken));
    }
}