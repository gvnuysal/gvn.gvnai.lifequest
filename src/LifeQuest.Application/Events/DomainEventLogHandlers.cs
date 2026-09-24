using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Application.Events;

/// <summary>
/// GvnDbContext domain event'leri commit'ten SONRA yayınlar. Burada fırlatılan bir hata, başarıyla yazılmış
/// bir isteği 500'e çevirir; bu yüzden handler'lar yalnızca yan etkisiz iş yapar (log, ileride bildirim
/// kuyruğa alma). Güvenilir yan etkiler gerektiğinde Outbox'a geçilmelidir.
/// </summary>
internal sealed class DomainEventLogHandlers(ILogger<DomainEventLogHandlers> logger) :
    INotificationHandler<UserRegisteredEvent>,
    INotificationHandler<OnboardingCompletedEvent>,
    INotificationHandler<LifeLevelUpEvent>,
    INotificationHandler<AchievementUnlockedEvent>
{
    public Task Handle(UserRegisteredEvent e, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} registered", e.UserId);
        return Task.CompletedTask;
    }

    public Task Handle(OnboardingCompletedEvent e, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} completed onboarding", e.UserId);
        return Task.CompletedTask;
    }

    public Task Handle(LifeLevelUpEvent e, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} reached life level {Level}", e.UserId, e.NewLevel);
        return Task.CompletedTask;
    }

    public Task Handle(AchievementUnlockedEvent e, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} unlocked achievement {Code}", e.UserId, e.Code);
        return Task.CompletedTask;
    }
}
