using LifeQuest.Domain.Common;
using LifeQuest.Domain.Progression;

namespace LifeQuest.Application.Progression;

public sealed record AchievementDto(string Code, string Title, string Description, bool Unlocked, DateTime? UnlockedAt);

public sealed record CategoryProgressDto(
    LifeCategory Category, string DisplayName, int Xp, int Level, int NextLevelXp, int CompletedCount);

public sealed record XpEntryDto(DateTime At, string Description, int LifeXp, LifeCategory Category, int CategoryXp);

public sealed record ProgressDto(
    int LifeXp,
    int LifeLevel,
    int CurrentLevelXp,
    int NextLevelXp,
    double LevelProgress,
    int TotalCompleted,
    IReadOnlyList<CategoryProgressDto> Categories,
    IReadOnlyList<XpEntryDto> RecentXp);

public static class ProgressMappings
{
    public static AchievementDto ToDto(this AchievementDefinition definition, DateTime? unlockedAt)
        => new(definition.Code, definition.Title.Current, definition.Description.Current, unlockedAt is not null, unlockedAt);
}
