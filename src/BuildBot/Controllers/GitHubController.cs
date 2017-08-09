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
        private ILogger _logger;

        public GitHubController(IPublisher<Push> pushPublisher, ILogger logger)
        {
            this._pushPublisher = pushPublisher;
            this._logger = logger;
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
            try
            {
                await this._pushPublisher.Publish(request);
            }
            catch (Exception ex)
            {
                this._logger.LogError(new EventId(ex.HResult), ex.Message, ex);
            }

            return NoContent();
        }
    }
}
