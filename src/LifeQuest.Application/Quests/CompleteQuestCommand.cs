using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Application.Progression;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Quests;

/// <summary>
/// Tamamlama kullanıcı onayıdır; fotoğraf veya konum zorunlu değildir. Quest durumu, XP, kategori ilerlemesi,
/// başarımlar, XP defteri kaydı ve ilgi öğrenmesi tek bir transaction'da yazılır. İdempotenttir:
/// tekrar çağrı çift XP üretmez; eşzamanlı çağrılar optimistic concurrency ile 409 alır.
/// </summary>
public sealed record CompleteQuestCommand(Guid QuestId) : ICommand<QuestCompletionDto>;

internal sealed class CompleteQuestCommandHandler(
    IUserQuestRepository quests,
    IPlayerProgressRepository progressRepository,
    IUserProfileRepository profiles,
    IUserContext user,
    IUnitOfWork unitOfWork,
    LifeQuestMetrics metrics,
    TimeProvider clock) : ICommandHandler<CompleteQuestCommand, QuestCompletionDto>
{
    public async Task<Result<QuestCompletionDto>> Handle(CompleteQuestCommand command, CancellationToken cancellationToken)
    {
        var userId = user.UserId;
        var now = clock.GetUtcNow().UtcDateTime;

        var quest = await quests.GetForUserAsync(command.QuestId, userId, cancellationToken);
        if (quest is null)
            return Result<QuestCompletionDto>.Fail(QuestErrors.NotFound);

        var progress = await progressRepository.GetByUserIdAsync(userId, cancellationToken);
        if (progress is null)
            return Result<QuestCompletionDto>.Fail(ProfileErrors.ProfileNotFound);

        var completion = quest.Complete(now);
        if (!completion.Succeeded)
            return Result<QuestCompletionDto>.Fail(completion.Errors);

        if (!completion.Data)
            return Result<QuestCompletionDto>.Ok(new QuestCompletionDto(
                quest.ToDto(), AlreadyCompleted: true, progress.LifeXp, progress.LifeLevel, false, []));

        var outcome = progress.ApplyQuestReward(quest, now);
        await progressRepository.AddTransactionAsync(outcome.Transaction, cancellationToken);

        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        profile?.AdjustInterests(quest.InterestIds, InterestLearning.CompletionDelta, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        metrics.Completed(quest.Category, quest.Reward.LifeXp);

        return Result<QuestCompletionDto>.Ok(new QuestCompletionDto(
            quest.ToDto(), AlreadyCompleted: false, progress.LifeXp, progress.LifeLevel, outcome.LeveledUp,
            outcome.NewAchievements.Select(a => a.ToDto(now)).ToList()));
    }
}
