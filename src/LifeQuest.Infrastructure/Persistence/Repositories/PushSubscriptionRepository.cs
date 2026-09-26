using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class PushSubscriptionRepository(LifeQuestDbContext context)
    : EfRepository<PushSubscription, LifeQuestDbContext>(context), IPushSubscriptionRepository
{
    public Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);

    public async Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetSubscribedUserIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
        => (await DbSet.AsNoTracking()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
}
