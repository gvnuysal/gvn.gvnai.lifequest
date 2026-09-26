namespace LifeQuest.Domain.Notifications;

/// <summary>
/// Haftalık özet tercihi. "Bugün uygulamaya girmedin" türü geri çağırma yoktur. Günlük push hatırlatması ayrıdır
/// ve kullanıcı saat seçerek açar (<see cref="DailyReminder"/>, <c>UserProfile.DailyReminderHour</c>).
/// </summary>
public enum NotificationPreference
{
    Off = 0,
    WeeklySummary = 1
}
