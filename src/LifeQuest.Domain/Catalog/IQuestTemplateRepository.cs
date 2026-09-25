using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Catalog;

public interface IQuestTemplateRepository : IRepository<QuestTemplate>
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<QuestTemplate> Items, int TotalCount)> SearchAsync(
        TemplateSearch search, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Önerilebilir (aktif ve güvenli) template'lerin tanımları: katalog dengesi kontrolü için.</summary>
    Task<IReadOnlyList<QuestTemplateSpec>> GetOfferableSpecsAsync(CancellationToken cancellationToken = default);

    Task<int> CountBySafetyAsync(SafetyLevel safety, CancellationToken cancellationToken = default);
}

public sealed record TemplateSearch(
    string? Text = null,
    LifeCategory? Category = null,
    QuestType? Type = null,
    SafetyLevel? Safety = null,
    bool? IsActive = null);
