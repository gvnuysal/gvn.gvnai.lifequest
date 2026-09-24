using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize]
[Route("api/v1/summaries")]
public sealed class SummariesController(ISender sender) : ApiControllerBase
{
    /// <summary>Okunmamış son haftalık özet; yoksa <c>null</c>.</summary>
    [HttpGet("latest")]
    public async Task<IActionResult> Latest(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetLatestSummaryQuery(), cancellationToken));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new MarkSummaryReadCommand(id), cancellationToken));
}
