using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Identity;

public interface IUserAccountRepository : IRepository<UserAccount>
{
    Task<UserAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Token yeniden kullanımı tespit edildiğinde tüm aileyi anında iptal eder (UoW dışında çalışır).</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, string reason, CancellationToken cancellationToken = default);
}
