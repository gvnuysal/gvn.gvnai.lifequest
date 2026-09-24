using Gvn.GvnFramework.EntityFramewokCore.Repositories;
using LifeQuest.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class UserAccountRepository(LifeQuestDbContext context)
    : EfRepository<UserAccount, LifeQuestDbContext>(context), IUserAccountRepository
{
    public Task<UserAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
}

internal sealed class RefreshTokenRepository(LifeQuestDbContext context)
    : EfRepository<RefreshToken, LifeQuestDbContext>(context), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, string reason, CancellationToken cancellationToken = default)
        => DbSet
            .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.RevokedAt, nowUtc)
                .SetProperty(x => x.RevokeReason, reason), cancellationToken);
}
