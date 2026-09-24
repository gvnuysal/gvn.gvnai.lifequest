using LifeQuest.Application.Progression;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Application.Quests;

public sealed record QuestRewardDto(int LifeXp, int PrimaryCategoryXp, int SecondaryCategoryXp);

public sealed record QuestDto(
    Guid Id,
    string Title,
    string Description,
    QuestType Type,
    Difficulty Difficulty,
    LifeCategory Category,
    LifeCategory? SecondaryCategory,
    int MinMinutes,
    int MaxMinutes,
    CostBand Cost,
    QuestRewardDto Reward,
    QuestStatus Status,
    QuestSource Source,
    DateTime OfferedAt,
    DateTime ExpiresAt,
    DateTime? AcceptedAt,
    DateTime? CompletedAt,
    SkipReason? SkipReason,
    int? Rating,
    FeedbackPreference? Preference,
    bool IsExploration,
    string Explanation);

/// <summary>"Neden bunu önerdim?" detayı: skor bileşenleri ve gerekçe kodları.</summary>
public sealed record QuestDetailDto(QuestDto Quest, ScoreBreakdown Score, IReadOnlyList<string> ReasonCodes);

public sealed record QuestListDto(DateOnly Date, IReadOnlyList<QuestDto> Quests, string? Message);

public sealed record QuestCompletionDto(
    QuestDto Quest,
    bool AlreadyCompleted,
    int LifeXp,
    int LifeLevel,
    bool LeveledUp,
    IReadOnlyList<AchievementDto> NewAchievements);

public sealed record QuestFeedbackDto(QuestDto Quest, IReadOnlyList<AchievementDto> NewAchievements);

public static class QuestMappings
{
    public static QuestDto ToDto(this UserQuest q) => new(
        q.Id, q.Title, q.Description, q.Type, q.Difficulty, q.Category, q.SecondaryCategory,
        q.MinMinutes, q.MaxMinutes, q.Cost,
        new QuestRewardDto(q.Reward.LifeXp, q.Reward.PrimaryCategoryXp, q.Reward.SecondaryCategoryXp),
        q.Status, q.Source, q.OfferedAt, q.ExpiresAt, q.AcceptedAt, q.CompletedAt, q.SkipReason,
        q.Rating, q.Preference, q.IsExploration, q.Explanation);

    public static IReadOnlyList<QuestDto> ToDtos(this IEnumerable<UserQuest> quests)
        => quests.OrderBy(q => q.Slot).Select(ToDto).ToList();
}
