using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.RealWorld;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.RealWorld;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

/// <summary>Şehir bazında mekân ve etkinlikler; görev template'lerine bağlanır.</summary>
[Authorize(Roles = UserRoles.Admin)]
[Route("api/v1/admin/places")]
public sealed class AdminPlacesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? city, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new ListLocalPlacesQuery(city), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(PlaceRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(request.ToCommand(null), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PlaceRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new DeleteLocalPlaceCommand(id), cancellationToken));

    public sealed record PlaceRequest(
        LocalPlaceKind Kind,
        string City,
        string Name,
        string? Address,
        string? Url,
        string? Note,
        DateTime? StartsAt,
        DateTime? EndsAt,
        IReadOnlyList<Guid>? TemplateIds,
        bool IsActive = true)
    {
        public SaveLocalPlaceCommand ToCommand(Guid? id) => new(
            id, Kind, City, Name, Address, Url, Note,
            StartsAt?.ToUniversalTime(), EndsAt?.ToUniversalTime(), TemplateIds ?? [], IsActive);
    }
}
