using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Quests;

public sealed record SkipQuestCommand(Guid QuestId, SkipReason Reason) : ICommand<QuestDto>;

public sealed class SkipQuestCommandValidator : AbstractValidator<SkipQuestCommand>
{
    public SkipQuestCommandValidator() => RuleFor(x => x.Reason).IsInEnum();
}

internal sealed class SkipQuestCommandHandler(
    IUserQuestRepository quests,
    IUserProfileRepository profiles,
    IUserContext user,
    IUnitOfWork unitOfWork,
    LifeQuestMetrics metrics,
    TimeProvider clock) : ICommandHandler<SkipQuestCommand, QuestDto>
{
    public async Task<Result<QuestDto>> Handle(SkipQuestCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var quest = await quests.GetForUserAsync(command.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<QuestDto>.Fail(QuestErrors.NotFound);

        var wasOpen = quest.IsOpen;
        var result = quest.Skip(command.Reason, now);
        if (!result.Succeeded)
            return Result<QuestDto>.Fail(result.Errors);

        // Yalnızca "ilgimi çekmedi" ilgi sinyalidir; pahalı / zamanım yok / uzak sürtünme (friction) bilgisidir.
        if (wasOpen && command.Reason == SkipReason.NotInterested)
        {
            var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
            profile?.AdjustInterests(quest.InterestIds, InterestLearning.NotInterestedDelta, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (wasOpen) metrics.Skipped(command.Reason);

        return Result<QuestDto>.Ok(quest.ToDto());
    }
}
