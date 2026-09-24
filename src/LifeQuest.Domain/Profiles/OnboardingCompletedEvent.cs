using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Profiles;

public sealed record OnboardingCompletedEvent(Guid UserId) : LifeQuestDomainEvent;
