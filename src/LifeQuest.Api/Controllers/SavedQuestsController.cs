using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Quests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

/// <summary>"Sonra yaparım" listesi.</summary>
[Authorize]
[Route("api/v1/saved")]
public sealed class SavedQuestsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetSavedQuestsQuery(), cancellationToken));

    [HttpDelete("{templateId:guid}")]
    public async Task<IActionResult> Remove(Guid templateId, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new RemoveSavedQuestCommand(templateId), cancellationToken));

    /// <summary>Deneyimi başlatır: kabul edilmiş bir quest olur ve listeden çıkar.</summary>
    [HttpPost("{templateId:guid}/start")]
    public async Task<IActionResult> Start(Guid templateId, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new StartSavedQuestCommand(templateId), cancellationToken));
}
