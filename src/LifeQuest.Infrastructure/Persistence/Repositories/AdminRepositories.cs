using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class AdminAuditRepository(LifeQuestDbContext context)
    : EfRepository<AdminAuditEntry, LifeQuestDbContext>(context), IAdminAuditRepository
{
    public async Task<(IReadOnlyList<AdminAuditEntry> Items, int TotalCount)> GetPageAsync(
        AdminAction? action, AdminTargetType? targetType, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (action is not null)
            query = query.Where(e => e.Action == action);
        if (targetType is not null)
            query = query.Where(e => e.TargetType == targetType);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(e => e.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}

internal sealed class RecommendationSettingsRepository(LifeQuestDbContext context)
    : EfRepository<RecommendationSettings, LifeQuestDbContext>(context), IRecommendationSettingsRepository
{
    public Task<RecommendationSettings?> GetAsync(CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.Id == RecommendationSettings.SingletonId, cancellationToken);
}

internal sealed class QuestTemplateRepository(LifeQuestDbContext context)
    : EfRepository<QuestTemplate, LifeQuestDbContext>(context), IQuestTemplateRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        => DbSet.IgnoreQueryFilters().AnyAsync(t => t.Code == code, cancellationToken);

    public async Task<(IReadOnlyList<QuestTemplate> Items, int TotalCount)> SearchAsync(
        TemplateSearch search, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (search.Text is { } text)
        {
            var pattern = $"%{EscapeLike(text)}%";
            query = query.Where(t => EF.Functions.ILike(t.Title, pattern, "\\") || EF.Functions.ILike(t.Code, pattern, "\\"));
        }
        if (search.Category is { } category)
            query = query.Where(t => t.Category == category);
        if (search.Type is { } type)
            query = query.Where(t => t.Type == type);
        if (search.Safety is { } safety)
            query = query.Where(t => t.Safety == safety);
        if (search.IsActive is { } active)
            query = query.Where(t => t.IsActive == active);

        var total = await query.CountAsync(cancellationToken);
        // İnceleme bekleyenler önce: admin'in yapması gereken iş listenin başında görünür.
        var items = await query
            .OrderBy(t => t.Safety == SafetyLevel.NeedsReview ? 0 : 1)
            .ThenBy(t => t.Category).ThenBy(t => t.Title)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<QuestTemplateSpec>> GetOfferableSpecsAsync(CancellationToken cancellationToken = default)
        => (await DbSet.AsNoTracking().Where(t => t.IsActive && t.Safety == SafetyLevel.Safe).ToListAsync(cancellationToken))
            .Select(t => t.ToSpec())
            .ToList();

    public Task<int> CountBySafetyAsync(SafetyLevel safety, CancellationToken cancellationToken = default)
        => DbSet.CountAsync(t => t.Safety == safety, cancellationToken);

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
