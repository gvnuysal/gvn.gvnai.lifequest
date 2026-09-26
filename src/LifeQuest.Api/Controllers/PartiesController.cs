using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Api.Infrastructure;
using LifeQuest.Application.Social;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeQuest.Api.Controllers;

/// <summary>
/// Quest Party davetleri. Kod tahminine karşı davet uçları kimlik doğrulama gibi sınırlandırılır; önizleme yalnızca
/// kurucunun görünen adını ve üye sayısını gösterir.
/// </summary>
[Authorize]
[Route("api/v1/parties")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class PartiesController(ISender sender) : ApiControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> Invite(string code, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetPartyInviteQuery(code), cancellationToken));

    [HttpPost("{code}/join")]
    public async Task<IActionResult> Join(string code, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new JoinPartyCommand(code), cancellationToken));

    [HttpDelete("{code}/members/me")]
    public async Task<IActionResult> Leave(string code, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new LeavePartyCommand(code), cancellationToken));
}
