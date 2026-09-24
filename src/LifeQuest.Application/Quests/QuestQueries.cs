using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Application.Common;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Quests;

public sealed record GetActiveQuestsQuery : IQuery<IReadOnlyList<QuestDto>>;

internal sealed class GetActiveQuestsQueryHandler(IUserQuestRepository quests, IUserContext user, TimeProvider clock)
    : IQueryHandler<GetActiveQuestsQuery, IReadOnlyList<QuestDto>>
{
    public async Task<Result<IReadOnlyList<QuestDto>>> Handle(GetActiveQuestsQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var accepted = await quests.GetAcceptedAsync(user.UserId, cancellationToken);

        return Result<IReadOnlyList<QuestDto>>.Ok(accepted
            .Where(q => q.ExpiresAt > now)
            .OrderBy(q => q.ExpiresAt)
            .Select(q => q.ToDto())
            .ToList());
    }
}

public sealed record GetQuestQuery(Guid QuestId) : IQuery<QuestDetailDto>;

internal sealed class GetQuestQueryHandler(IUserQuestRepository quests, IUserContext user)
    : IQueryHandler<GetQuestQuery, QuestDetailDto>
{
    public async Task<Result<QuestDetailDto>> Handle(GetQuestQuery query, CancellationToken cancellationToken)
    {
        var quest = await quests.GetForUserAsync(query.QuestId, user.UserId, cancellationToken);
        return quest is null
            ? Result<QuestDetailDto>.Fail(QuestErrors.NotFound)
            : Result<QuestDetailDto>.Ok(new QuestDetailDto(quest.ToDto(), quest.Score, quest.ReasonCodes));
    }
}

public sealed record GetQuestHistoryQuery(int PageNumber, int PageSize, QuestStatus? Status) : IQuery<PagedResult<QuestDto>>;

public sealed class GetQuestHistoryQueryValidator : AbstractValidator<GetQuestHistoryQuery>
{
    public GetQuestHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Status).IsInEnum();
    }
}

internal sealed class GetQuestHistoryQueryHandler(IUserQuestRepository quests, IUserContext user)
    : IQueryHandler<GetQuestHistoryQuery, PagedResult<QuestDto>>
{
    public async Task<Result<PagedResult<QuestDto>>> Handle(GetQuestHistoryQuery query, CancellationToken cancellationToken)
    {
        // Framework PagedRequest sayfa boyutunu 1-100 aralığına sabitler.
        var paging = new PagedRequest { PageNumber = query.PageNumber, PageSize = query.PageSize };

        var (items, total) = await quests.GetHistoryPageAsync(
            user.UserId, query.Status, paging.Skip, paging.PageSize, cancellationToken);

        return Result<PagedResult<QuestDto>>.Ok(new PagedResult<QuestDto>(
            items.Select(q => q.ToDto()), total, paging.PageNumber, paging.PageSize));
    }
}
