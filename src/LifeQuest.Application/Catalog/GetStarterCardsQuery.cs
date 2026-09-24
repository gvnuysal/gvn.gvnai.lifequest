using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;

namespace LifeQuest.Application.Catalog;

public sealed record StarterCardDto(
    string Code, string Title, string Description, LifeCategory Category, CostBand Cost, int MinMinutes, int MaxMinutes);

/// <summary>Cold start: "Bunlardan hangisi sana göre?" kartları. Kategoriler sırayla karıştırılır ki ilk kartlar çeşitli olsun.</summary>
public sealed record GetStarterCardsQuery : IQuery<IReadOnlyList<StarterCardDto>>;

internal sealed class GetStarterCardsQueryHandler(IQuestCatalog catalog)
    : IQueryHandler<GetStarterCardsQuery, IReadOnlyList<StarterCardDto>>
{
    public async Task<Result<IReadOnlyList<StarterCardDto>>> Handle(GetStarterCardsQuery query, CancellationToken cancellationToken)
    {
        var cards = await catalog.GetStarterCardsAsync(cancellationToken);

        var interleaved = cards
            .GroupBy(c => c.Category)
            .OrderBy(g => g.Key)
            .SelectMany(g => g.OrderBy(c => c.Code, StringComparer.Ordinal).Select((card, index) => (card, index)))
            .OrderBy(x => x.index).ThenBy(x => x.card.Category)
            .Select(x => new StarterCardDto(
                x.card.Code, x.card.Title, x.card.Description, x.card.Category, x.card.Cost, x.card.MinMinutes, x.card.MaxMinutes))
            .ToList();

        return Result<IReadOnlyList<StarterCardDto>>.Ok(interleaved);
    }
}
