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

    /// <summary>Onboarding cold start kartları (editoryal olarak işaretlenmiş, önerilebilir template'ler).</summary>
    Task<IReadOnlyList<StarterCard>> GetStarterCardsAsync(CancellationToken cancellationToken = default);
}

public sealed record StarterCard(
    Guid TemplateId,
    string Code,
    string Title,
    string Description,
    LifeCategory Category,
    CostBand Cost,
    int MinMinutes,
    int MaxMinutes,
    IReadOnlyList<Guid> InterestIds);

public sealed record InterestCatalogItem(Guid Id, string Code, string Name, LifeCategory Category);
