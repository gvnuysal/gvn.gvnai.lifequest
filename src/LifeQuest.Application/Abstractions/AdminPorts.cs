using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Application.Abstractions;

/// <summary>Konfigürasyondaki ağırlıklar + admin override'ları (cache'li). Öneri motoru ağırlıkları buradan alır.</summary>
public interface IRecommendationWeightsProvider
{
    Task<RecommendationWeights> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcıya özel ağırlıklar: çalışan bir A/B deneyi varsa kullanıcı deterministik olarak bir gruba atanır;
    /// Deneme grubundaysa deneme override'ları üretim ağırlıklarının üzerine uygulanır.
    /// </summary>
    Task<UserWeights> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task InvalidateAsync(CancellationToken cancellationToken = default);
}

public sealed record UserWeights(RecommendationWeights Weights, Guid? ExperimentId, ExperimentVariant? Variant);

/// <summary>
/// Hesabın güncel rolü ve askı durumu. Her kimlikli istekte okunur (kısa süreli cache); admin işlemi sonrası temizlenir.
/// Böylece askıya alma ve rol değişikliği access token'ın süresini beklemeden etkili olur.
/// </summary>
public interface IAccountStateCache
{
    /// <returns>Hesap silinmişse <c>null</c>.</returns>
    Task<AccountState?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record AccountState(string Role, DateTime? SuspendedAt, DateTime? SuspendedUntil)
{
    public bool IsSuspended(DateTime nowUtc) => SuspendedAt is not null && (SuspendedUntil is null || SuspendedUntil > nowUtc);
}

/// <summary>Admin kullanıcı listesi için salt okunur projeksiyon: yalnızca hesap bilgisi ve toplam sayılar.</summary>
public interface IAdminUserReader
{
    Task<(IReadOnlyList<AdminUserRow> Items, int TotalCount)> SearchAsync(
        string? search, AdminUserFilter filter, DateTime nowUtc, int skip, int take, CancellationToken cancellationToken = default);

    Task<AdminUserRow?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

public enum AdminUserFilter
{
    All = 0,
    Admins = 1,
    Suspended = 2
}

public sealed record AdminUserRow(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime? SuspendedAt,
    DateTime? SuspendedUntil,
    string? SuspensionReason,
    int CompletedQuests,
    int LifeXp);
