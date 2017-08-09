using BuildBot.Discord.Publishers;
using BuildBot.ServiceModel.GitHub;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BuildBot.Controllers
{
    [Route("[controller]")]
    public class GitHubController : Controller
    {
        private IPublisher<Push> _pushPublisher;

        public GitHubController(IPublisher<Push> pushPublisher)
        {
            this._pushPublisher = pushPublisher;
        }

        [HttpPost]
        [Route("ping")]
        public async Task<IActionResult> Post([FromBody] Ping request)
        {
            await Task.CompletedTask;

            return NoContent();
        }

        [HttpPost]
        [Route("push")]
        public async Task<IActionResult> Push([FromBody] Push request)
        {
            await this._pushPublisher.Publish(request);

            return NoContent();
        }
    }
}
