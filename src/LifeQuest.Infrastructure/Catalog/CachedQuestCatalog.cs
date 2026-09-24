using Gvn.GvnFramework.Caching.Abstractions;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Recommendations;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Catalog;

/// <summary>
/// Katalog nadiren değişir ve her öneri üretiminde okunur: framework <see cref="ICacheService"/> ile
/// (Memory veya Redis) önbelleğe alınır. Önbellekte entity değil, serileştirilebilir snapshot tutulur.
/// </summary>
internal sealed class CachedQuestCatalog(LifeQuestDbContext db, ICacheService cache) : IQuestCatalog
{
    private const string CacheKey = "lifequest:catalog:v2";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<QuestCandidate>> GetOfferableCandidatesAsync(CancellationToken cancellationToken = default)
        => (await GetSnapshotAsync(cancellationToken)).Candidates;

    public async Task<TasteGraph> GetTasteGraphAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        return new TasteGraph(snapshot.Edges, snapshot.Interests.Select(i => new InterestInfo(i.Id, i.Code, i.Name)));
    }

    public async Task<IReadOnlyList<InterestCatalogItem>> GetInterestsAsync(CancellationToken cancellationToken = default)
        => (await GetSnapshotAsync(cancellationToken)).Interests;

    public async Task<IReadOnlyList<StarterCard>> GetStarterCardsAsync(CancellationToken cancellationToken = default)
        => (await GetSnapshotAsync(cancellationToken)).Starters;

    private Task<CatalogSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        => cache.GetOrSetAsync(CacheKey, () => LoadAsync(cancellationToken), CacheDuration, cancellationToken);

    private async Task<CatalogSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        var templates = await db.QuestTemplates.AsNoTracking()
            .Where(t => t.IsActive && t.Safety == Domain.Catalog.SafetyLevel.Safe)
            .ToListAsync(cancellationToken);

        var candidates = templates
            .Where(t => t.IsOfferable)
            .Select(t => new QuestCandidate(
                t.Id, t.Code, t.Version, t.Title, t.Description, t.Type, t.Difficulty, t.Category, t.SecondaryCategory,
                t.MinMinutes, t.MaxMinutes, t.Cost, t.DayParts, t.RequiresCity, t.RiskScore, t.CooldownDays,
                t.InterestIds, t.IsOutdoor, t.Effort))
            .ToList();

        var starters = templates
            .Where(t => t.IsOfferable && t.IsStarter)
            .Select(t => new StarterCard(
                t.Id, t.Code, t.Title, t.Description, t.Category, t.Cost, t.MinMinutes, t.MaxMinutes, t.InterestIds))
            .ToList();

        var interests = await db.Interests.AsNoTracking()
            .Where(i => i.IsActive)
            .Select(i => new InterestCatalogItem(i.Id, i.Code, i.Name, i.Category))
            .ToListAsync(cancellationToken);

        var edges = await db.InterestRelations.AsNoTracking()
            .Select(r => new InterestEdge(r.FromInterestId, r.ToInterestId, r.Weight, r.Confidence))
            .ToListAsync(cancellationToken);

        return new CatalogSnapshot(candidates, edges, interests, starters);
    }

    internal sealed record CatalogSnapshot(
        List<QuestCandidate> Candidates,
        List<InterestEdge> Edges,
        List<InterestCatalogItem> Interests,
        List<StarterCard> Starters);
}
