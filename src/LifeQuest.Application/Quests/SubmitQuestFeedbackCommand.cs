using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Progression;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Quests;

/// <summary>Beğeni 1-5 (yalnızca tamamlananlar) ve/veya "buna benzer daha fazla / daha az göster".</summary>
public sealed record SubmitQuestFeedbackCommand(Guid QuestId, int? Rating, FeedbackPreference? Preference)
    : ICommand<QuestFeedbackDto>;

public sealed class SubmitQuestFeedbackCommandValidator : AbstractValidator<SubmitQuestFeedbackCommand>
{
    public SubmitQuestFeedbackCommandValidator()
    {
        RuleFor(x => x).Must(x => x.Rating is not null || x.Preference is not null)
            .WithMessage(_ => Text.Of("Değerlendirme veya tercih bilgisinden en az biri gönderilmelidir.", "Send a rating, a preference, or both."))
            .OverridePropertyName("Feedback");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Preference).IsInEnum();
    }
}

internal sealed class SubmitQuestFeedbackCommandHandler(
    IUserQuestRepository quests,
    IUserProfileRepository profiles,
    IPlayerProgressRepository progressRepository,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<SubmitQuestFeedbackCommand, QuestFeedbackDto>
{
    public async Task<Result<QuestFeedbackDto>> Handle(SubmitQuestFeedbackCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var quest = await quests.GetForUserAsync(command.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<QuestFeedbackDto>.Fail(QuestErrors.NotFound);

        var previousPreference = quest.Preference;
        var feedback = quest.RecordFeedback(command.Rating, command.Preference, now);
        if (!feedback.Succeeded)
            return Result<QuestFeedbackDto>.Fail(feedback.Errors);

        // Öğrenme sinyalleri yalnızca ilk kez verildiğinde uygulanır; aynı geri bildirimi tekrarlayarak
        // ilgi ağırlıkları şişirilemez.
        var delta = 0d;
        if (feedback.Data && command.Rating is { } rating)
            delta += InterestLearning.FromRating(rating);
        if (command.Preference is { } preference && preference != previousPreference)
            delta += preference == FeedbackPreference.MoreLikeThis
                ? InterestLearning.MoreLikeThisDelta
                : InterestLearning.LessLikeThisDelta;

        if (delta != 0)
        {
            var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
            profile?.AdjustInterests(quest.InterestIds, delta, now);
        }

        IReadOnlyList<AchievementDefinition> unlocked = [];
        if (feedback.Data)
        {
            var progress = await progressRepository.GetByUserIdAsync(user.UserId, cancellationToken);
            if (progress is not null)
                unlocked = progress.RegisterFeedback(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<QuestFeedbackDto>.Ok(new QuestFeedbackDto(quest.ToDto(), unlocked.Select(a => a.ToDto(now)).ToList()));
    }
}
