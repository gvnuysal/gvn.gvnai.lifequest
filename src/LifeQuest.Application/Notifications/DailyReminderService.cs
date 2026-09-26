using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Quests;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Notifications;

/// <summary>
/// Hatırlatma saati gelen kullanıcılara günde bir push gönderir. O görev gününde zaten görev tamamlamış kullanıcıya
/// gönderilmez; yine de gün "işlendi" sayılır ki saat içinde tekrar denenmesin.
/// </summary>
public sealed class DailyReminderService(
    IUserProfileRepository profiles,
    IPushSubscriptionRepository subscriptions,
    IUserQuestRepository quests,
    PushNotifier notifier,
    IUnitOfWork unitOfWork,
    IOptions<QuestOptions> questOptions,
    TimeProvider clock,
    ILogger<DailyReminderService> logger)
{
    public sealed record RunResult(int Due, int Sent, int SkippedAlreadyDone);

    public async Task<RunResult> SendDueAsync(CancellationToken cancellationToken)
    {
        if (!notifier.IsConfigured)
            return new RunResult(0, 0, 0);

        var candidates = await profiles.GetDailyReminderUserIdsAsync(cancellationToken);
        var subscribed = await subscriptions.GetSubscribedUserIdsAsync(candidates, cancellationToken);

        int due = 0, sent = 0, skipped = 0;
        foreach (var userId in candidates.Where(subscribed.Contains))
        {
            try
            {
                var outcome = await SendForUserAsync(userId, cancellationToken);
                if (outcome is null) continue;
                due++;
                if (outcome == true) sent++;
                else skipped++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Daily reminder failed for {UserId}", userId);
            }
        }

        return new RunResult(due, sent, skipped);
    }

    /// <returns>null: vakti değil · true: gönderildi · false: bugün zaten görev tamamlanmış.</returns>
    private async Task<bool?> SendForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        if (profile is not { OnboardingCompleted: true } || !profile.IsDailyReminderDue(nowUtc))
            return null;

        var timeZone = profile.ResolveTimeZone();
        var dayStartHour = questOptions.Value.DayStartHour;
        var questDay = TimeZones.QuestDay(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone), dayStartHour);
        var questDayStartUtc = TimeZones.EndOfQuestDayUtc(questDay.AddDays(-1), timeZone, dayStartHour);

        profile.MarkDailyReminderHandled(nowUtc);

        var doneToday = (await quests.GetCompletedBetweenAsync(userId, questDayStartUtc, nowUtc, cancellationToken)).Count > 0;
        if (!doneToday)
        {
            var active = await quests.CountAcceptedAsync(userId, cancellationToken);
            var offers = (await quests.GetOffersAsync(userId, questDay, QuestSource.Daily, cancellationToken))
                .Count(q => q.Status == QuestStatus.Offered);
            var (title, body) = DailyReminder.Compose(active, offers);
            await notifier.SendToUserAsync(userId, new PushNotification(title, body, "/today", "daily-reminder"), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return !doneToday;
    }
}
