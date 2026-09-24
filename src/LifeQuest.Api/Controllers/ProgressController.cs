using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Progression;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize]
[Route("api/v1")]
public sealed class ProgressController(ISender sender) : ApiControllerBase
{
    /// <summary>Life XP, seviye ve Life Profile kategori ilerlemesi.</summary>
    [HttpGet("progress")]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetProgressQuery(), cancellationToken));

    [HttpGet("achievements")]
    public async Task<IActionResult> Achievements(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetAchievementsQuery(), cancellationToken));
}
