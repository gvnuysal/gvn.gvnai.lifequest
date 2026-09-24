using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Admin;
using LifeQuest.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize(Roles = UserRoles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminController(ISender sender) : ApiControllerBase
{
    /// <summary>North-star ve ürün funnel metrikleri (anonim, toplu sayımlar).</summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics([FromQuery] int days = 7, CancellationToken cancellationToken = default)
        => HandleResult(await sender.Send(new GetProductMetricsQuery(days), cancellationToken));
}
