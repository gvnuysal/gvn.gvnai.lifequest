using LifeQuest.Application.Admin;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence;

/// <summary>Ürün metrikleri: user_quests üzerinde periyot bazlı SQL aggregate'leri (salt okunur).</summary>
internal sealed class ProductMetricsReader(LifeQuestDbContext db) : IProductMetricsReader
{
    public async Task<ProductMetricsSnapshot> ReadAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var quests = db.UserQuests.AsNoTracking();

        var offered = quests.Where(q => q.OfferedAt >= fromUtc && q.OfferedAt < toUtc);
        var accepted = quests.Where(q => q.AcceptedAt >= fromUtc && q.AcceptedAt < toUtc);
        var completed = quests.Where(q => q.Status == QuestStatus.Completed && q.CompletedAt >= fromUtc && q.CompletedAt < toUtc);
        var skipped = quests.Where(q => q.Status == QuestStatus.Skipped && q.SkippedAt >= fromUtc && q.SkippedAt < toUtc);

        var activeUsers = await accepted.Select(q => q.UserId)
            .Union(completed.Select(q => q.UserId))
            .Distinct()
            .CountAsync(cancellationToken);

        var skipReasons = (await skipped
                .GroupBy(q => q.SkipReason)
                .Select(g => new { Reason = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
            .Where(x => x.Reason is not null)
            .ToDictionary(x => x.Reason!.Value, x => x.Count);

        var byCategory = (await completed
                .GroupBy(q => q.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Category, x => x.Count);

        return new ProductMetricsSnapshot(
            activeUsers,
            await offered.CountAsync(cancellationToken),
            await accepted.CountAsync(cancellationToken),
            await completed.CountAsync(cancellationToken),
            await completed.CountAsync(q => q.Rating == null || q.Rating >= 4, cancellationToken),
            await completed.CountAsync(q => q.Reward.NoveltyMultiplier == RewardCalculator.NewCategoryMultiplier, cancellationToken),
            await offered.CountAsync(q => q.IsExploration, cancellationToken),
            await offered.CountAsync(q => q.IsExploration && q.AcceptedAt != null, cancellationToken),
            await completed.Where(q => q.Rating != null).AverageAsync(q => (double?)q.Rating, cancellationToken),
            skipReasons,
            byCategory);
    }
}
