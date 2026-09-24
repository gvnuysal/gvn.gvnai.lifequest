using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[Authorize]
[Route("api/v1/onboarding")]
public sealed class OnboardingController(ISender sender) : ApiControllerBase
{
    /// <summary>Cold start: "Bunlardan hangisi sana göre?" kartları.</summary>
    [HttpGet("starter-cards")]
    public async Task<IActionResult> StarterCards(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetStarterCardsQuery(), cancellationToken));
}
