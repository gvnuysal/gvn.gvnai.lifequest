using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Admin.Users;
using LifeQuest.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize(Roles = UserRoles.Admin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search, [FromQuery] AdminUserFilter filter = AdminUserFilter.All,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => HandleResult(await sender.Send(new SearchUsersQuery(search, filter, pageNumber, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetUserQuery(id), cancellationToken));

    /// <summary>Hesabı askıya alır ve açık oturumlarını kapatır.</summary>
    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, SuspendRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SuspendUserCommand(id, request.Days, request.Reason), cancellationToken));

    [HttpPost("{id:guid}/unsuspend")]
    public async Task<IActionResult> Unsuspend(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new UnsuspendUserCommand(id), cancellationToken));

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, RoleRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SetUserRoleCommand(id, request.Role), cancellationToken));

    /// <summary>Hesabı ve tüm verisini kalıcı olarak siler. Gerekçe ve e-posta onayı zorunludur.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new DeleteUserCommand(id, request.Reason, request.ConfirmEmail), cancellationToken));

    public sealed record SuspendRequest(int? Days, string Reason);

    public sealed record RoleRequest(string Role);

    public sealed record DeleteRequest(string Reason, string ConfirmEmail);
}
