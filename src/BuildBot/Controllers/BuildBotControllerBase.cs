using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildBot.Controllers;

[Route(template: "[controller]")]
public abstract class BuildBotControllerBase : ControllerBase
{
    protected BuildBotControllerBase(ILogger logger)
    {
        this.Logger = logger;
    }

    protected ILogger Logger { get; }

    protected async Task<IActionResult> ProcessAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            this.Logger.LogError(new(exception.HResult), exception: exception, message: exception.Message);
        }

        return this.NoContent();
    }
}