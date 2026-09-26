using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository(LifeQuestDbContext context)
    : EfRepository<UserProfile, LifeQuestDbContext>(context), IUserProfileRepository
{
    public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet.Include(x => x.Interests).FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
}

internal sealed class PlayerProgressRepository : EfRepository<PlayerProgress, LifeQuestDbContext>, IPlayerProgressRepository
{
    // Framework EfRepository context'i dışarı açmadığı için diğer DbSet'ler adına ayrıca tutulur.
    private readonly LifeQuestDbContext _db;

    public PlayerProgressRepository(LifeQuestDbContext db) : base(db) => _db = db;

    public Task<PlayerProgress?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet
            .Include(x => x.Categories)
            .Include(x => x.Achievements)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddTransactionAsync(XpTransaction transaction, CancellationToken cancellationToken = default)
        => await _db.XpTransactions.AddAsync(transaction, cancellationToken);

    public async Task<IReadOnlyList<XpTransaction>> GetRecentTransactionsAsync(
        Guid userId, int take, CancellationToken cancellationToken = default)
        => await _db.XpTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
}
