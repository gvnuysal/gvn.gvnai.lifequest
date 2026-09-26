using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Notifications;

/// <summary>
/// Kullanıcının yerel takvimine göre geçen haftanın (pazartesi-pazar) uygulama içi özetini üretir.
/// İdempotenttir: aynı hafta için ikinci kez çağrılırsa bir şey yapmaz.
/// </summary>
public sealed class WeeklySummaryService(
    IUserProfileRepository profiles,
    IUserQuestRepository quests,
    IWeeklySummaryRepository summaries,
    Domain.Identity.IUserAccountRepository accounts,
    PushNotifier notifier,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    /// <returns>Yeni özet oluşturulduysa <c>true</c>.</returns>
    public async Task<bool> GenerateForPreviousWeekAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is not { OnboardingCompleted: true, NotificationPreference: NotificationPreference.WeeklySummary })
            return false;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var timeZone = profile.ResolveTimeZone();
        var (weekStart, fromUtc, toUtc) = PreviousWeek(nowUtc, timeZone);

        // Hafta bittikten sonra kaydolan kullanıcıya "sakin geçti" demek anlamsız olur.
        if (profile.CreatedAt >= toUtc || await summaries.ExistsAsync(userId, weekStart, cancellationToken))
            return false;

        var completed = await quests.GetCompletedBetweenAsync(userId, fromUtc, toUtc, cancellationToken);
        var stats = new WeeklyStats(
            completed.Count,
            completed.Sum(q => q.Reward.LifeXp),
            completed.Where(q => q.Reward.NoveltyMultiplier == RewardCalculator.NewCategoryMultiplier)
                .Select(q => q.Category).Distinct().ToList(),
            completed.Count >= 2
                ? completed.GroupBy(q => q.Category).OrderByDescending(g => g.Count()).First().Key
                : null,
            await quests.CountAcceptedAsync(userId, cancellationToken));

        var summary = WeeklySummary.Create(userId, weekStart, stats, nowUtc);
        await summaries.AddAsync(summary, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Push izni veren kullanıcıya özet ayrıca bildirim olarak gider; izin yoksa yalnızca uygulama içinde görünür.
        var language = await accounts.GetLanguageAsync(userId, cancellationToken);
        var (title, message) = summary.Text;
        if (await notifier.SendToUserAsync(userId, new PushNotification(title.In(language), message.In(language), "/today", "weekly-summary"),
                cancellationToken) > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static (DateOnly WeekStart, DateTime FromUtc, DateTime ToUtc) PreviousWeek(DateTime nowUtc, TimeZoneInfo timeZone)
    {
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        var daysSinceMonday = ((int)localToday.DayOfWeek + 6) % 7;
        var thisMonday = localToday.AddDays(-daysSinceMonday);
        var previousMonday = thisMonday.AddDays(-7);

        return (previousMonday, ToUtc(previousMonday), ToUtc(thisMonday));

        DateTime ToUtc(DateOnly date)
            => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
    }
}
