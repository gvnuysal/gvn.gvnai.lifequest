using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Progression;

public interface IPlayerProgressRepository : IRepository<PlayerProgress>
{
    /// <summary>Kategori ilerlemesi ve başarımlarla birlikte, değişiklik takibi açık yükler.</summary>
    Task<PlayerProgress?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddTransactionAsync(XpTransaction transaction, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<XpTransaction>> GetRecentTransactionsAsync(Guid userId, int take, CancellationToken cancellationToken = default);
}
