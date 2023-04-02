using System.Net;
using BuildBot.Helpers;
using BuildBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace BuildBot.Controllers;

public sealed class PingController : ControllerBase
{
    [HttpGet]
    [Produces(contentType: "application/json")]
    [ProducesResponseType(typeof(PongDto), (int)HttpStatusCode.OK)]
    public IActionResult Get()
    {
        return this.Ok(PingPong.Model);
    }
}