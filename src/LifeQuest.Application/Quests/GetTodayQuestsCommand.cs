using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;

namespace LifeQuest.Application.Quests;

/// <summary>Bugünün önerileri; yoksa üretilir (bu yüzden query değil command). Tekrar çağrı aynı sonucu döner.</summary>
public sealed record GetTodayQuestsCommand : ICommand<QuestListDto>;

internal sealed class GetTodayQuestsCommandHandler(QuestOfferService offers, IUserContext user)
    : ICommandHandler<GetTodayQuestsCommand, QuestListDto>
{
    public async Task<Result<QuestListDto>> Handle(GetTodayQuestsCommand command, CancellationToken cancellationToken)
    {
        var result = await offers.GetOrCreateDailyOffersAsync(user.UserId, cancellationToken);
        return result.Succeeded
            ? Result<QuestListDto>.Ok(new QuestListDto(result.Data!.Date, result.Data.Quests.ToDtos(), result.Data.Message, result.Data.Weather))
            : Result<QuestListDto>.Fail(result.Errors);
    }
}
