using Gvn.GvnFramework.Caching.Abstractions;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Recommendations;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeQuest.Infrastructure.Admin;

/// <summary>Konfigürasyon ağırlıkları + admin override'ları. Güncellemede cache düşürülür; öneriler hemen yeni değerlerle üretilir.</summary>
internal sealed class RecommendationWeightsProvider(
    LifeQuestDbContext db, ICacheService cache, IOptions<RecommendationWeights> defaults) : IRecommendationWeightsProvider
{
    private const string CacheKey = "lifequest:weights:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<RecommendationWeights> GetAsync(CancellationToken cancellationToken = default)
    {
        var overrides = await cache.GetOrSetAsync(CacheKey, async () =>
        {
            var settings = await db.RecommendationSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == RecommendationSettings.SingletonId, cancellationToken);
            return settings?.Overrides ?? new Dictionary<string, double>();
        }, CacheDuration, cancellationToken);

        return RecommendationWeightCatalog.Apply(defaults.Value, overrides);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default) => cache.RemoveAsync(CacheKey, cancellationToken);
}

/// <summary>Her kimlikli istekte okunur; kısa ömürlü cache ile veritabanı yükü düşük tutulur.</summary>
internal sealed class AccountStateCache(LifeQuestDbContext db, ICacheService cache) : IAccountStateCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<AccountState?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entry = await cache.GetOrSetAsync(Key(userId), async () => new CachedState(
            await db.UserAccounts.AsNoTracking()
                .Where(a => a.Id == userId)
                .Select(a => new AccountState(a.Role, a.SuspendedAt, a.SuspendedUntil))
                .FirstOrDefaultAsync(cancellationToken)), CacheDuration, cancellationToken);
        return entry.State;
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(Key(userId), cancellationToken);

    private static string Key(Guid userId) => $"lifequest:account-state:{userId:N}";

    /// <summary>Silinmiş hesap da (State = null) cache'lenir; aksi halde her istekte veritabanına gidilirdi.</summary>
    internal sealed record CachedState(AccountState? State);
}
