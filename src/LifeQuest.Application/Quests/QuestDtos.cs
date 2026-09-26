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
    PhysicalEffort Effort,
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
    string Explanation,
    DateTime? PlannedAt);

/// <summary>"Neden bunu önerdim?" detayı: skor bileşenleri ve gerekçe kodları.</summary>
/// <param name="NearbyPlaces">Kullanıcının şehrinde bu göreve bağlı mekân ve yaklaşan etkinlikler.</param>
/// <param name="Party">Görev bir Quest Party'deyse üyeler ve durum.</param>
public sealed record QuestDetailDto(
    QuestDto Quest, ScoreBreakdown Score, IReadOnlyList<string> ReasonCodes, IReadOnlyList<RealWorld.NearbyPlaceDto> NearbyPlaces,
    Social.PartyDto? Party = null);

/// <param name="Weather">Kullanıcının şehrinde hava; şehir yoksa ya da alınamadıysa null.</param>
public sealed record QuestListDto(DateOnly Date, IReadOnlyList<QuestDto> Quests, string? Message, RealWorld.WeatherDto? Weather = null);

public sealed record QuestCompletionDto(
    QuestDto Quest,
    bool AlreadyCompleted,
    int LifeXp,
    int LifeLevel,
    bool LeveledUp,
    IReadOnlyList<AchievementDto> NewAchievements,
    int PartyBonusXp = 0);

public sealed record QuestFeedbackDto(QuestDto Quest, IReadOnlyList<AchievementDto> NewAchievements);

public static class QuestMappings
{
    public static QuestDto ToDto(this UserQuest q) => new(
        q.Id, q.Title, q.Description, q.Type, q.Difficulty, q.Category, q.SecondaryCategory,
        q.MinMinutes, q.MaxMinutes, q.Cost, q.Effort,
        new QuestRewardDto(q.Reward.LifeXp, q.Reward.PrimaryCategoryXp, q.Reward.SecondaryCategoryXp),
        q.Status, q.Source, q.OfferedAt, q.ExpiresAt, q.AcceptedAt, q.CompletedAt, q.SkipReason,
        q.Rating, q.Preference, q.IsExploration, q.Explanation, q.PlannedAt);

    public static IReadOnlyList<QuestDto> ToDtos(this IEnumerable<UserQuest> quests)
        => quests.OrderBy(q => q.Slot).Select(ToDto).ToList();
}
