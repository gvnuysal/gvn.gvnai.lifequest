using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Community;
using LifeQuest.Application.Experiments;
using LifeQuest.Domain.Community;
using LifeQuest.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize(Roles = UserRoles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminExperimentsController(ISender sender) : ApiControllerBase
{
    [HttpGet("experiments")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new ListExperimentsQuery(), cancellationToken));

    /// <summary>Deney ve (başladıysa) Kontrol / Deneme karşılaştırması.</summary>
    [HttpGet("experiments/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetExperimentQuery(id), cancellationToken));

    [HttpPost("experiments")]
    public async Task<IActionResult> Create(CreateExperimentCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    /// <summary>start · stop · adopt (deneme ağırlıklarını üretime al) · discard</summary>
    [HttpPost("experiments/{id:guid}/{command:regex(^(start|stop|adopt|discard)$)}")]
    public async Task<IActionResult> Change(
        Guid id, string command, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ChangeRequest? request,
        CancellationToken cancellationToken)
        => HandleResult(await sender.Send(
            new ChangeExperimentCommand(id, Enum.Parse<ExperimentCommand>(command, ignoreCase: true), request?.Reason), cancellationToken));

    [HttpGet("ideas")]
    public async Task<IActionResult> Ideas(
        [FromQuery] IdeaStatus? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await sender.Send(new SearchIdeasQuery(status, pageNumber, pageSize), cancellationToken));

    [HttpGet("ideas/{id:guid}")]
    public async Task<IActionResult> Idea(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetIdeaQuery(id), cancellationToken));

    /// <summary>Fikri reddeder; not kullanıcıya gösterilir.</summary>
    [HttpPost("ideas/{id:guid}/reject")]
    public async Task<IActionResult> RejectIdea(Guid id, RejectRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new RejectIdeaCommand(id, request.Note), cancellationToken));

    public sealed record ChangeRequest(string? Reason);

    public sealed record RejectRequest(string Note);
}
