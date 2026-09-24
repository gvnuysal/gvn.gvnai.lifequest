using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Identity;

public sealed record UserRegisteredEvent(Guid UserId) : LifeQuestDomainEvent;
