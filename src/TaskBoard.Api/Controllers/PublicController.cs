using Microsoft.AspNetCore.Mvc;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController : ControllerBase
{
    [HttpGet("ping")]
    public ActionResult<PingResponse> Ping()
    {
        return Ok(new PingResponse("ok"));
    }
}

public sealed record PingResponse(string Status);
