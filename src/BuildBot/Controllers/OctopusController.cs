using System;
using System.Threading.Tasks;
using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.Octopus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers
{
    [Route(template: "[controller]")]
    public sealed class OctopusController : Controller
    {
        private readonly IPublisher<Deploy> _deployPublisher;
        private readonly ILogger _logger;

        public OctopusController(IPublisher<Deploy> deployPublisher, ILogger logger)
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
            catch (Exception ex)
            {
                this._logger.LogError(new EventId(ex.HResult), ex.Message, ex);
            }

            return this.Ok();
        }

        [HttpPost]
        [Route(template: "deploy")]
        public Task<IActionResult> PushAsync([FromBody] Deploy request)
        {
            return this.ProcessAsync(action: () => this._deployPublisher.PublishAsync(request));
        }
    }
}