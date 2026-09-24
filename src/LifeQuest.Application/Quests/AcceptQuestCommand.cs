using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Domain.Quests;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Quests;

public sealed record AcceptQuestCommand(Guid QuestId) : ICommand<QuestDto>;

internal sealed class AcceptQuestCommandHandler(
    IUserQuestRepository quests,
    IUserContext user,
    IUnitOfWork unitOfWork,
    IOptions<QuestOptions> options,
    LifeQuestMetrics metrics,
    TimeProvider clock) : ICommandHandler<AcceptQuestCommand, QuestDto>
{
    public async Task<Result<QuestDto>> Handle(AcceptQuestCommand command, CancellationToken cancellationToken)
    {
        var quest = await quests.GetForUserAsync(command.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<QuestDto>.Fail(QuestErrors.NotFound);

        if (quest.Status == QuestStatus.Offered &&
            await quests.CountAcceptedAsync(user.UserId, cancellationToken) >= options.Value.MaxActiveQuests)
            return Result<QuestDto>.Fail(QuestErrors.TooManyActiveQuests(options.Value.MaxActiveQuests));

        var wasOffered = quest.Status == QuestStatus.Offered;
        var result = quest.Accept(clock.GetUtcNow().UtcDateTime);
        if (!result.Succeeded)
            return Result<QuestDto>.Fail(result.Errors);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (wasOffered) metrics.Accepted(quest.Category);

        return Result<QuestDto>.Ok(quest.ToDto());
    }
}
