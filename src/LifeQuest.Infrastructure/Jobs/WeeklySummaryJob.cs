using Gvn.GvnFramework.BackgroundJobs.Abstractions;
using LifeQuest.Application.Notifications;
using LifeQuest.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LifeQuest.Infrastructure.Persistence;

namespace LifeQuest.Infrastructure.Jobs;

/// <summary>
/// Pazartesi sabahı (UTC 05:00 ≈ İstanbul 08:00) haftalık özet tercihini açık tutan kullanıcılar için geçen
/// haftanın uygulama içi özetini üretir. İdempotent: aynı hafta için ikinci kez özet oluşmaz.
/// </summary>
public sealed class WeeklySummaryJob(IServiceScopeFactory scopeFactory, ILogger<WeeklySummaryJob> logger) : IRecurringJob
{
    public const string JobId = "notifications:weekly-summary";
    public const string Cron = "0 5 * * 1";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> userIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            userIds = await scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>().UserProfiles.AsNoTracking()
                .Where(p => p.OnboardingCompleted && p.NotificationPreference == NotificationPreference.WeeklySummary)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);
        }

        var created = 0;
        foreach (var userId in userIds)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            try
            {
                if (await scope.ServiceProvider.GetRequiredService<WeeklySummaryService>()
                        .GenerateForPreviousWeekAsync(userId, cancellationToken))
                    created++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Weekly summary failed for {UserId}", userId);
            }
        }

        logger.LogInformation("Weekly summaries created: {Created}/{Total}", created, userIds.Count);
    }
}
