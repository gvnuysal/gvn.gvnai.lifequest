using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Identity;
using LifeQuest.Application.Profiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LifeQuest.Api.Infrastructure;

namespace LifeQuest.Api.Controllers;

[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetMyProfileQuery(), cancellationToken));

    [HttpPut("onboarding")]
    public async Task<IActionResult> CompleteOnboarding(CompleteOnboardingCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdatePreferences(UpdatePreferencesCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    [HttpPut("interests")]
    public async Task<IActionResult> SetInterests(SetInterestsCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    /// <summary>Hesabı ve tüm verileri kalıcı olarak siler (KVKK/GDPR).</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(DeleteAccountCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    /// <summary>KVKK/GDPR veri taşınabilirliği: tüm verilerini JSON dosyası olarak indirir (saatte 3 istek).</summary>
    [HttpGet("export")]
    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportMyDataQuery(), cancellationToken);
        return result.Succeeded
            ? File(result.Data!.Content, "application/json", result.Data.FileName)
            : HandleResult(result);
    }
}
