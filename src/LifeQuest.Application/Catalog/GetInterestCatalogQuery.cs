using LifeQuest.Domain.Localization;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;

namespace LifeQuest.Application.Catalog;

public sealed record InterestDto(Guid Id, string Code, string Name, LifeCategory Category);

public sealed record GetInterestCatalogQuery : IQuery<IReadOnlyList<InterestDto>>;

internal sealed class GetInterestCatalogQueryHandler(IQuestCatalog catalog)
    : IQueryHandler<GetInterestCatalogQuery, IReadOnlyList<InterestDto>>
{
    public async Task<Result<IReadOnlyList<InterestDto>>> Handle(GetInterestCatalogQuery query, CancellationToken cancellationToken)
    {
        var interests = await catalog.GetInterestsAsync(cancellationToken);
        return Result<IReadOnlyList<InterestDto>>.Ok(interests
            .Select(i => new InterestDto(i.Id, i.Code, i.DisplayName(), i.Category))
            .OrderBy(i => i.Category).ThenBy(i => i.Name, StringComparer.Create(Language.CurrentCulture, false))
            .ToList());
    }
}
