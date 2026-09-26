using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Profiles;

public interface IUserProfileRepository : IRepository<UserProfile>
{
    /// <summary>İlgi alanlarıyla birlikte, değişiklik takibi açık yükler.</summary>
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Günlük hatırlatması açık kullanıcıların kimlikleri (ilgi alanları yüklenmez).</summary>
    Task<IReadOnlyList<Guid>> GetDailyReminderUserIdsAsync(CancellationToken cancellationToken = default);
}
