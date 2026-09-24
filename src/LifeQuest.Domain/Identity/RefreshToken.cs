using Gvn.GvnFramework.Domain.Entities;

namespace LifeQuest.Domain.Identity;

/// <summary>
/// Refresh token rotation: her kullanımda yenisi üretilir, eskisi iptal edilir. Aynı ailede (FamilyId)
/// iptal edilmiş bir token tekrar kullanılırsa token çalınmış kabul edilir ve tüm aile iptal edilir.
/// Token'ın kendisi değil, SHA-256 özeti saklanır.
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public Guid FamilyId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokeReason { get; private set; }
    public Guid? ReplacedById { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(Guid userId, string tokenHash, Guid familyId, DateTime nowUtc, TimeSpan lifetime)
        => new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.Add(lifetime)
        };

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTime nowUtc) => !IsRevoked && ExpiresAt > nowUtc;

    public void Revoke(DateTime nowUtc, string reason, Guid? replacedById = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = nowUtc;
        RevokeReason = reason;
        ReplacedById = replacedById;
    }
}
