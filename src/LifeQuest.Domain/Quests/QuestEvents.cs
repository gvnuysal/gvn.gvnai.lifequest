using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Quests;

public sealed record QuestAcceptedEvent(Guid QuestId, Guid UserId) : LifeQuestDomainEvent;

public sealed record QuestCompletedEvent(Guid QuestId, Guid UserId, LifeCategory Category, int LifeXp) : LifeQuestDomainEvent;

public sealed record QuestSkippedEvent(Guid QuestId, Guid UserId, SkipReason Reason) : LifeQuestDomainEvent;
