using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Admin.Catalog;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize(Roles = UserRoles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminCatalogController(ISender sender) : ApiControllerBase
{
    [HttpGet("templates")]
    public async Task<IActionResult> Search(
        [FromQuery] string? text, [FromQuery] LifeCategory? category, [FromQuery] QuestType? type,
        [FromQuery] SafetyLevel? safety, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => HandleResult(await sender.Send(
            new SearchTemplatesQuery(text, category, type, safety, isActive, pageNumber, pageSize), cancellationToken));

    [HttpGet("templates/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetTemplateQuery(id), cancellationToken));

    /// <summary>Yeni template. Editoryal kurallara uymuyorsa NeedsReview olarak kaydedilir ve önerilmez.</summary>
    [HttpPost("templates")]
    public async Task<IActionResult> Create(
        TemplateInput template, [FromQuery] Guid? sourceIdeaId, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new CreateTemplateCommand(template, sourceIdeaId), cancellationToken));

    [HttpPut("templates/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new UpdateTemplateCommand(id, request.Version, request.Template), cancellationToken));

    [HttpPost("templates/{id:guid}/safety")]
    public async Task<IActionResult> SetSafety(Guid id, SafetyRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SetTemplateSafetyCommand(id, request.Safety, request.Note), cancellationToken));

    [HttpPost("templates/{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SetTemplateActiveCommand(id, true), cancellationToken));

    [HttpPost("templates/{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SetTemplateActiveCommand(id, false), cancellationToken));

    /// <summary>Kaydetmeden editoryal kural kontrolü.</summary>
    [HttpPost("templates/validate")]
    public async Task<IActionResult> Validate(TemplateInput template, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new ValidateTemplateQuery(template), cancellationToken));

    [HttpGet("catalog/health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetCatalogHealthQuery(), cancellationToken));

    public sealed record UpdateRequest(int Version, TemplateInput Template);

    public sealed record SafetyRequest(SafetyLevel Safety, string? Note);
}
