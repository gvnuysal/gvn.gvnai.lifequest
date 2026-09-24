using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Progression;

public sealed record LifeLevelUpEvent(Guid UserId, int NewLevel) : LifeQuestDomainEvent;

public sealed record CategoryLevelUpEvent(Guid UserId, LifeCategory Category, int NewLevel) : LifeQuestDomainEvent;

public sealed record AchievementUnlockedEvent(Guid UserId, string Code) : LifeQuestDomainEvent;
