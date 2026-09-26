using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Domain.Entities;
using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Notifications;

/// <summary>
/// Bir tarayıcının Web Push aboneliği (push servisi adresi + şifreleme anahtarları). Kullanıcı başına birden çok cihaz
/// olabilir. Push servisi aboneliği "yok" (404/410) derse silinir; art arda başarısızlıkta da bırakılır.
/// </summary>
public sealed class PushSubscription : Entity
{
    public const int MaxPerUser = 10;
    public const int MaxConsecutiveFailures = 5;

    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = default!;
    public string P256dh { get; private set; } = default!;
    public string Auth { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastSuccessAt { get; private set; }
    public int ConsecutiveFailures { get; private set; }

    private PushSubscription() { }

    public static PushSubscription Create(Guid userId, string endpoint, string p256dh, string auth, DateTime nowUtc) => new()
    {
        UserId = userId,
        Endpoint = Guard.NotNullOrWhiteSpace(endpoint, nameof(endpoint)),
        P256dh = Guard.NotNullOrWhiteSpace(p256dh, nameof(p256dh)),
        Auth = Guard.NotNullOrWhiteSpace(auth, nameof(auth)),
        CreatedAt = nowUtc
    };

    /// <summary>Aynı tarayıcıda başka hesapla oturum açıldığında abonelik yeni hesaba geçer; anahtarlar yenilenmiş olabilir.</summary>
    public void Reassign(Guid userId, string p256dh, string auth)
    {
        UserId = userId;
        P256dh = p256dh;
        Auth = auth;
        ConsecutiveFailures = 0;
    }

    public void RecordSuccess(DateTime nowUtc)
    {
        LastSuccessAt = nowUtc;
        ConsecutiveFailures = 0;
    }

    /// <returns>Abonelik artık bırakılmalı mı.</returns>
    public bool RecordFailure() => ++ConsecutiveFailures >= MaxConsecutiveFailures;
}

public interface IPushSubscriptionRepository : IRepository<PushSubscription>
{
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Verilen kullanıcılardan en az bir aboneliği olanlar.</summary>
    Task<IReadOnlySet<Guid>> GetSubscribedUserIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
}
