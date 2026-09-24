using LifeQuest.Domain.Common;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Application.Abstractions;

/// <summary>Quest kataloğu ve Taste Graph için okuma portu (Infrastructure'da cache'li uygulanır).</summary>
public interface IQuestCatalog
{
    /// <summary>Yalnızca aktif ve güvenli (Safety = Safe) template'ler.</summary>
    Task<IReadOnlyList<QuestCandidate>> GetOfferableCandidatesAsync(CancellationToken cancellationToken = default);

    Task<TasteGraph> GetTasteGraphAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterestCatalogItem>> GetInterestsAsync(CancellationToken cancellationToken = default);
}

public sealed record InterestCatalogItem(Guid Id, string Code, string Name, LifeCategory Category);
