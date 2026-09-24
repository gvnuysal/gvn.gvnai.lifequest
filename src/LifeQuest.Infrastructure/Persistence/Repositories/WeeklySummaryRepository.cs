using Gvn.GvnFramework.EntityFramewokCore.Repositories;
using LifeQuest.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class WeeklySummaryRepository(LifeQuestDbContext context)
    : EfRepository<WeeklySummary, LifeQuestDbContext>(context), IWeeklySummaryRepository
{
    public Task<bool> ExistsAsync(Guid userId, DateOnly weekStart, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.UserId == userId && x.WeekStart == weekStart, cancellationToken);

    public Task<WeeklySummary?> GetLatestUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.ReadAt == null)
            .OrderByDescending(x => x.WeekStart)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<WeeklySummary?> GetForUserAsync(Guid summaryId, Guid userId, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.Id == summaryId && x.UserId == userId, cancellationToken);
}
