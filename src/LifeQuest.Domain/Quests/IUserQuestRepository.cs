using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Quests;

public interface IUserQuestRepository : IRepository<UserQuest>
{
    /// <summary>Sahiplik kontrolüyle yükler: başka kullanıcının quest'i için null döner (yatay erişim koruması).</summary>
    Task<UserQuest?> GetForUserAsync(Guid questId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserQuest>> GetOffersAsync(Guid userId, DateOnly offerDate, QuestSource source, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserQuest>> GetAcceptedAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountAcceptedAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<UserQuest> Items, int TotalCount)> GetHistoryPageAsync(
        Guid userId, QuestStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuestHistoryItem>> GetHistoryItemsAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    /// <summary>Açık (Offered/Accepted) quest'lerin template id'leri — aynı quest iki kez sunulmaz.</summary>
    /// <summary>Kullanıcının bu template için açık (önerilmiş ya da kabul edilmiş, süresi dolmamış) görevi.</summary>
    Task<UserQuest?> GetOpenForTemplateAsync(Guid userId, Guid templateId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetOpenTemplateIdsAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Tüm zamanlarda tamamlanan template'ler ve son tamamlanma anı (cooldown + novelty).</summary>
    Task<IReadOnlyDictionary<Guid, DateTime>> GetCompletedTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Çok sevilen template'ler: en az bir kez 5 puan veya "daha fazla" almış, hiç düşük puan / "daha az" almamış.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetLovedTemplateIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserQuest>> GetCompletedBetweenAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserQuest>> GetExpirableAsync(DateTime nowUtc, int take, CancellationToken cancellationToken = default);

    /// <summary>Son <paramref name="sinceUtc"/> tarihinden beri quest kabul/tamamlamış, onboarding'i bitmiş kullanıcılar.</summary>
    Task<IReadOnlyList<Guid>> GetRecentlyActiveUserIdsAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}
