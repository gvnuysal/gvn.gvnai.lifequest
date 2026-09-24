using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Notifications;

public interface IWeeklySummaryRepository : IRepository<WeeklySummary>
{
    Task<bool> ExistsAsync(Guid userId, DateOnly weekStart, CancellationToken cancellationToken = default);

    Task<WeeklySummary?> GetLatestUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<WeeklySummary?> GetForUserAsync(Guid summaryId, Guid userId, CancellationToken cancellationToken = default);
}
