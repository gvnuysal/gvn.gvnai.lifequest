using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.Community;
using LifeQuest.Domain.Experiments;
using LifeQuest.Domain.Quests;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class SavedQuestRepository(LifeQuestDbContext context)
    : EfRepository<SavedQuest, LifeQuestDbContext>(context), ISavedQuestRepository
{
    public async Task<IReadOnlyList<SavedQuest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.Where(x => x.UserId == userId).OrderByDescending(x => x.SavedAt).ToListAsync(cancellationToken);

    public Task<SavedQuest?> GetAsync(Guid userId, Guid templateId, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.UserId == userId && x.TemplateId == templateId, cancellationToken);

    public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet.CountAsync(x => x.UserId == userId, cancellationToken);
}

internal sealed class ExperimentRepository(LifeQuestDbContext context)
    : EfRepository<Experiment, LifeQuestDbContext>(context), IExperimentRepository
{
    public Task<Experiment?> GetRunningAsync(CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.Status == ExperimentStatus.Running, cancellationToken);

    public async Task<IReadOnlyList<Experiment>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .OrderBy(x => x.Status == ExperimentStatus.Running ? 0 : x.Status == ExperimentStatus.Draft ? 1 : 2)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
}

internal sealed class QuestIdeaRepository(LifeQuestDbContext context)
    : EfRepository<QuestIdea, LifeQuestDbContext>(context), IQuestIdeaRepository
{
    public async Task<IReadOnlyList<QuestIdea>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.SubmittedAt).ToListAsync(cancellationToken);

    public Task<int> CountPendingAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet.CountAsync(x => x.UserId == userId && x.Status == IdeaStatus.Pending, cancellationToken);

    public Task<int> CountSubmittedSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        => DbSet.CountAsync(x => x.UserId == userId && x.SubmittedAt >= sinceUtc, cancellationToken);

    public async Task<(IReadOnlyList<QuestIdea> Items, int TotalCount)> GetPageAsync(
        IdeaStatus? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (status is not null)
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync(cancellationToken);
        // Bekleyenler en eskiden yeniye (kuyruk), diğerleri en yeniden eskiye.
        var items = await (status == IdeaStatus.Pending
                ? query.OrderBy(x => x.SubmittedAt)
                : query.OrderByDescending(x => x.SubmittedAt))
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> CountByStatusAsync(IdeaStatus status, CancellationToken cancellationToken = default)
        => DbSet.CountAsync(x => x.Status == status, cancellationToken);
}
