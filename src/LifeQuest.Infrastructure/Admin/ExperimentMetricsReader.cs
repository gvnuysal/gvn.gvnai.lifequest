using LifeQuest.Application.Experiments;
using LifeQuest.Domain.Quests;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Admin;

/// <summary>Deney grubuna yazılmış görevlerden kullanıcı başına sayımlar (tek SQL GROUP BY).</summary>
internal sealed class ExperimentMetricsReader(LifeQuestDbContext db) : IExperimentMetricsReader
{
    public async Task<IReadOnlyDictionary<ExperimentVariant, IReadOnlyList<UserExperimentStats>>> ReadAsync(
        Guid experimentId, CancellationToken cancellationToken = default)
    {
        var rows = await db.UserQuests.AsNoTracking()
            .Where(q => q.ExperimentId == experimentId && q.ExperimentVariant != null)
            .GroupBy(q => new { q.ExperimentVariant, q.UserId })
            .Select(g => new
            {
                Variant = g.Key.ExperimentVariant!.Value,
                g.Key.UserId,
                Offered = g.Count(),
                Accepted = g.Count(q => q.AcceptedAt != null),
                Completed = g.Count(q => q.Status == QuestStatus.Completed),
                Meaningful = g.Count(q => q.Status == QuestStatus.Completed && (q.Rating == null || q.Rating >= 4)),
                ExplorationOffered = g.Count(q => q.IsExploration),
                ExplorationAccepted = g.Count(q => q.IsExploration && q.AcceptedAt != null),
                NotInterested = g.Count(q => q.SkipReason == SkipReason.NotInterested),
                RatingSum = g.Sum(q => q.Rating ?? 0),
                RatingCount = g.Count(q => q.Rating != null)
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.Variant)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UserExperimentStats>)g.Select(r => new UserExperimentStats(
                    r.UserId, r.Offered, r.Accepted, r.Completed, r.Meaningful, r.ExplorationOffered, r.ExplorationAccepted,
                    r.NotInterested, r.RatingSum, r.RatingCount)).ToList());
    }
}
