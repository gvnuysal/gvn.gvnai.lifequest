using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Admin;
using LifeQuest.Application.Admin.Audit;
using LifeQuest.Application.Admin.Weights;
using LifeQuest.Domain.Admin;
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

    /// <summary>Admin işlemlerinin denetim kaydı (yeniden eskiye).</summary>
    [HttpGet("audit")]
    public async Task<IActionResult> Audit(
        [FromQuery] AdminAction? action, [FromQuery] AdminTargetType? targetType,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
        => HandleResult(await sender.Send(new GetAuditLogQuery(action, targetType, pageNumber, pageSize), cancellationToken));

    /// <summary>Öneri ağırlıkları: etkin değer, konfigürasyon varsayılanı ve sınırlar.</summary>
    [HttpGet("recommendation-weights")]
    public async Task<IActionResult> Weights(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetRecommendationWeightsQuery(), cancellationToken));

    /// <summary>Ağırlıkları günceller; yeni öneriler hemen bu değerlerle üretilir.</summary>
    [HttpPut("recommendation-weights")]
    public async Task<IActionResult> UpdateWeights(UpdateRecommendationWeightsCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    /// <summary>Seçili (veya tüm) ağırlıkları konfigürasyon varsayılanına döndürür.</summary>
    [HttpPost("recommendation-weights/reset")]
    public async Task<IActionResult> ResetWeights(ResetRecommendationWeightsCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));
}
