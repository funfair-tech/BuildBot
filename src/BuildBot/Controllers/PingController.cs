using System.Net;
using BuildBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace BuildBot.Controllers;

/// <summary>
///     Ping controller
/// </summary>
public sealed class PingController : ControllerBase
{
    /// <summary>
    ///     Gets the status.
    /// </summary>
    /// <returns>App status.</returns>
    [HttpGet]
    [Produces(contentType: "application/json")]
    [ProducesResponseType(typeof(PongDto), (int)HttpStatusCode.OK)]
    public IActionResult Get()
    {
        PongDto model = new("Pong!");

        return this.Ok(model);
    }
}