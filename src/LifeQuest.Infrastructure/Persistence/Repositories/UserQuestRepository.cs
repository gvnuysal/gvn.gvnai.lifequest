using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class UserQuestRepository : EfRepository<UserQuest, LifeQuestDbContext>, IUserQuestRepository
{
    private readonly LifeQuestDbContext _db;

    public UserQuestRepository(LifeQuestDbContext db) : base(db) => _db = db;

    public Task<UserQuest?> GetForUserAsync(Guid questId, Guid userId, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.Id == questId && x.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<UserQuest>> GetOffersAsync(
        Guid userId, DateOnly offerDate, QuestSource source, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(x => x.UserId == userId && x.OfferDate == offerDate && x.Source == source)
            .OrderBy(x => x.Slot)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserQuest>> GetAcceptedAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == QuestStatus.Accepted)
            .ToListAsync(cancellationToken);

    public Task<int> CountAcceptedAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet.CountAsync(x => x.UserId == userId && x.Status == QuestStatus.Accepted, cancellationToken);

    public async Task<(IReadOnlyList<UserQuest> Items, int TotalCount)> GetHistoryPageAsync(
        Guid userId, QuestStatus? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(x => x.UserId == userId);
        if (status is not null)
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.OfferedAt).ThenBy(x => x.Slot)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<QuestHistoryItem>> GetHistoryItemsAsync(
        Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.OfferedAt >= sinceUtc)
            .Select(x => new QuestHistoryItem(
                x.TemplateId, x.Category, x.InterestIds, x.Status, x.OfferedAt,
                x.CompletedAt ?? x.SkippedAt ?? x.ExpiredAt ?? x.AcceptedAt,
                x.SkipReason, x.Rating, x.Preference))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetOpenTemplateIdsAsync(
        Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId &&
                        (x.Status == QuestStatus.Offered || x.Status == QuestStatus.Accepted) &&
                        x.ExpiresAt > nowUtc)
            .Select(x => x.TemplateId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetCompletedTemplatesAsync(
        Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == QuestStatus.Completed)
            .GroupBy(x => x.TemplateId)
            .Select(g => new { TemplateId = g.Key, LastCompletedAt = g.Max(x => x.CompletedAt)!.Value })
            .ToDictionaryAsync(x => x.TemplateId, x => x.LastCompletedAt, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetLovedTemplateIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == QuestStatus.Completed)
            .GroupBy(x => x.TemplateId)
            .Where(g => g.Any(x => x.Rating == 5 || x.Preference == FeedbackPreference.MoreLikeThis) &&
                        !g.Any(x => x.Rating <= 2 || x.Preference == FeedbackPreference.LessLikeThis))
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserQuest>> GetCompletedBetweenAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == QuestStatus.Completed &&
                        x.CompletedAt >= fromUtc && x.CompletedAt < toUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserQuest>> GetExpirableAsync(
        DateTime nowUtc, int take, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(x => (x.Status == QuestStatus.Offered || x.Status == QuestStatus.Accepted) && x.ExpiresAt <= nowUtc)
            .OrderBy(x => x.ExpiresAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetRecentlyActiveUserIdsAsync(
        DateTime sinceUtc, CancellationToken cancellationToken = default)
        => await _db.UserProfiles.AsNoTracking()
            .Where(p => p.OnboardingCompleted &&
                        DbSet.Any(q => q.UserId == p.UserId && (q.AcceptedAt >= sinceUtc || q.CompletedAt >= sinceUtc)))
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);
}
