using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Community;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

/// <summary>Topluluk deneyim fikirleri: kullanıcı önerir, admin inceler.</summary>
[Authorize]
[Route("api/v1/ideas")]
public sealed class IdeasController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitIdeaCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetMyIdeasQuery(), cancellationToken));

    /// <summary>Henüz incelenmemiş kendi fikrini geri çeker.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new WithdrawIdeaCommand(id), cancellationToken));
}
