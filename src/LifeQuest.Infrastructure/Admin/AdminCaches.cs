using Gvn.GvnFramework.Caching.Abstractions;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Experiments;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeQuest.Infrastructure.Admin;

/// <summary>
/// Konfigürasyon ağırlıkları + admin override'ları + (varsa) çalışan A/B deneyinin deneme override'ları.
/// Override'lar ve çalışan deney cache'lenir; ayar veya deney değişince düşürülür, öneriler hemen yeni değerlerle üretilir.
/// </summary>
internal sealed class RecommendationWeightsProvider(
    LifeQuestDbContext db, ICacheService cache, IOptions<RecommendationWeights> defaults) : IRecommendationWeightsProvider
{
    private const string OverridesKey = "lifequest:weights:v1";
    private const string ExperimentKey = "lifequest:experiment:running:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<RecommendationWeights> GetAsync(CancellationToken cancellationToken = default)
    {
        var overrides = await cache.GetOrSetAsync(OverridesKey, async () =>
        {
            var settings = await db.RecommendationSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == RecommendationSettings.SingletonId, cancellationToken);
            return settings?.Overrides ?? new Dictionary<string, double>();
        }, CacheDuration, cancellationToken);

        return RecommendationWeightCatalog.Apply(defaults.Value, overrides);
    }

    public async Task<UserWeights> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var production = await GetAsync(cancellationToken);
        var running = await cache.GetOrSetAsync(ExperimentKey, async () => new RunningExperiment(
            await db.Experiments.AsNoTracking()
                .Where(e => e.Status == ExperimentStatus.Running)
                .Select(e => new ExperimentSnapshot(e.Id, e.TreatmentShare, e.TreatmentOverrides))
                .FirstOrDefaultAsync(cancellationToken)), CacheDuration, cancellationToken);

        if (running.Experiment is not { } experiment)
            return new UserWeights(production, null, null);

        var variant = ExperimentAssignment.VariantFor(userId, experiment.Id, experiment.TreatmentShare);
        var weights = variant == ExperimentVariant.Treatment
            ? RecommendationWeightCatalog.Apply(production, experiment.Overrides)
            : production;
        return new UserWeights(weights, experiment.Id, variant);
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(OverridesKey, cancellationToken);
        await cache.RemoveAsync(ExperimentKey, cancellationToken);
    }

    internal sealed record ExperimentSnapshot(Guid Id, double TreatmentShare, Dictionary<string, double> Overrides);

    /// <summary>"Deney yok" durumu da cache'lenir; aksi halde her öneride veritabanına gidilirdi.</summary>
    internal sealed record RunningExperiment(ExperimentSnapshot? Experiment);
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
