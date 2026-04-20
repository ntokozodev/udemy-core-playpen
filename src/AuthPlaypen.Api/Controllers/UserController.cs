using AuthPlaypen.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "admin")]
[Route("admin/api/[controller]")]
public class UserController(IActorContextService actorContextService) : ControllerBase
{
    [HttpGet]
    public ActionResult<ActorContext> GetCurrentUser()
    {
        var actor = actorContextService.GetCurrentActor();
        return Ok(actor);
    }
}
