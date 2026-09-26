using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class QuestPartyRepository(LifeQuestDbContext context)
    : EfRepository<QuestParty, LifeQuestDbContext>(context), IQuestPartyRepository
{
    public Task<QuestParty?> GetByCodeAsync(string inviteCode, CancellationToken cancellationToken = default)
        => DbSet.Include(p => p.Members).FirstOrDefaultAsync(p => p.InviteCode == inviteCode, cancellationToken);

    public Task<QuestParty?> GetByUserQuestAsync(Guid userQuestId, CancellationToken cancellationToken = default)
        => DbSet.Include(p => p.Members).FirstOrDefaultAsync(p => p.Members.Any(m => m.UserQuestId == userQuestId), cancellationToken);

    public async Task<IReadOnlyList<QuestParty>> GetByUserQuestsAsync(
        IReadOnlyCollection<Guid> userQuestIds, CancellationToken cancellationToken = default)
        => await DbSet.Include(p => p.Members)
            .Where(p => p.Members.Any(m => userQuestIds.Contains(m.UserQuestId)))
            .ToListAsync(cancellationToken);
}
