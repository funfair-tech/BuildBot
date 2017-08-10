using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.GitHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BuildBot.Controllers
{
    [Route("[controller]")]
    public class GitHubController : Controller
    {
        private IPublisher<Push> _pushPublisher;
        private IPublisher<Status> _statusPublisher;
        private ILogger _logger;

        public GitHubController(IPublisher<Push> pushPublisher, IPublisher<Status> statusPublisher, ILogger logger)
        {
            this._pushPublisher = pushPublisher;
            this._statusPublisher = statusPublisher;
            this._logger = logger;
        }

        private async Task<IActionResult> Process(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                this._logger.LogError(new EventId(ex.HResult), ex.Message, ex);
            }

            return NoContent();
        }

        [HttpPost]
        [Route("ping")]
        public IActionResult Ping([FromBody] Ping request)
        {
            return NoContent();
        }

        [HttpPost]
        [Route("push")]
        public async Task<IActionResult> Push([FromBody] Push request)
        {
            return await this.Process(() => this._pushPublisher.Publish(request));
        }

        [HttpPost]
        [Route("status")]
        public async Task<IActionResult> Status([FromBody] Status request)
        {
            return await this.Process(() => this._statusPublisher.Publish(request));
        }
    }
}
