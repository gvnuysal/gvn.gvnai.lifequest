using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

/// <summary>Web Push aboneliği. Hatırlatma saati profil tercihlerindedir (<c>dailyReminderHour</c>).</summary>
[Authorize]
[Route("api/v1/push")]
public sealed class PushController(ISender sender) : ApiControllerBase
{
    /// <summary>Push açık mı, tarayıcının abonelik için kullanacağı VAPID public key ve kayıtlı cihaz sayısı.</summary>
    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetPushSettingsQuery(), cancellationToken));

    [HttpPut("subscription")]
    public async Task<IActionResult> Subscribe(SubscriptionRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(
            new SubscribePushCommand(request.Endpoint, request.Keys?.P256dh ?? "", request.Keys?.Auth ?? ""), cancellationToken));

    [HttpDelete("subscription")]
    public async Task<IActionResult> Unsubscribe(UnsubscribeRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new UnsubscribePushCommand(request.Endpoint), cancellationToken));

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SendTestPushCommand(), cancellationToken));

    /// <summary>Tarayıcının <c>PushSubscription.toJSON()</c> biçimi.</summary>
    public sealed record SubscriptionRequest(string Endpoint, SubscriptionKeys? Keys);

    public sealed record SubscriptionKeys(string P256dh, string Auth);

    public sealed record UnsubscribeRequest(string Endpoint);
}
