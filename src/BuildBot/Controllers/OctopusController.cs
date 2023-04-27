using System.Threading;
using System.Threading.Tasks;
using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.Octopus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers;

public sealed class OctopusController : BuildBotControllerBase
{
    private readonly IPublisher<Deploy> _deployPublisher;

    public OctopusController(IPublisher<Deploy> deployPublisher, ILogger<OctopusController> logger)
        : base(logger: logger)
    {
        this._deployPublisher = deployPublisher;
    }

    [HttpPost]
    [Route(template: "deploy")]
    public Task<IActionResult> PushAsync([FromBody] Deploy request, CancellationToken cancellationToken)
    {
        return this.ProcessAsync(action: () => this._deployPublisher.PublishAsync(message: request, cancellationToken: cancellationToken));
    }
}