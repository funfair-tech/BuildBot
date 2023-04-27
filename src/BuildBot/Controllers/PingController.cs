using System.Net;
using BuildBot.Helpers;
using BuildBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers;

public sealed class PingController : BuildBotControllerBase
{
    public PingController(ILogger<PingController> logger)
        : base(logger)
    {
    }

    [HttpGet]
    [Produces(contentType: "application/json")]
    [ProducesResponseType(typeof(PongDto), (int)HttpStatusCode.OK)]
    public IActionResult Get()
    {
        return this.Ok(PingPong.Model);
    }
}