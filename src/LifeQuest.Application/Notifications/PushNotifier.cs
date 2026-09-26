using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Application.Notifications;

/// <summary>
/// Kullanıcının tüm cihazlarına push gönderir. Geçersiz abonelikler silinir; art arda başarısız olanlar da bırakılır.
/// Değişiklikleri kaydetmek çağıranın işidir (kendi SaveChanges'ıyla aynı işlemde).
/// </summary>
public sealed class PushNotifier(
    IPushSender sender,
    IPushSubscriptionRepository subscriptions,
    TimeProvider clock,
    ILogger<PushNotifier> logger)
{
    public bool IsConfigured => sender.IsConfigured;

    /// <returns>Bildirimin ulaştığı cihaz sayısı.</returns>
    public async Task<int> SendToUserAsync(Guid userId, PushNotification notification, CancellationToken cancellationToken)
    {
        if (!sender.IsConfigured)
            return 0;

        var delivered = 0;
        foreach (var subscription in await subscriptions.GetForUserAsync(userId, cancellationToken))
        {
            var result = await sender.SendAsync(
                new PushTarget(subscription.Endpoint, subscription.P256dh, subscription.Auth), notification, cancellationToken);

            switch (result)
            {
                case PushDelivery.Sent:
                    subscription.RecordSuccess(clock.GetUtcNow().UtcDateTime);
                    delivered++;
                    break;
                case PushDelivery.Expired:
                    await subscriptions.DeleteAsync(subscription, cancellationToken);
                    break;
                default:
                    if (subscription.RecordFailure())
                    {
                        logger.LogInformation("Dropping push subscription {SubscriptionId} after repeated failures", subscription.Id);
                        await subscriptions.DeleteAsync(subscription, cancellationToken);
                    }
                    break;
            }
        }

        return delivered;
    }
}
