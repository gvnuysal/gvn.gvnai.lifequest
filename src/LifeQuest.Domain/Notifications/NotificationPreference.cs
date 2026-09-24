namespace LifeQuest.Domain.Notifications;

/// <summary>
/// Kullanıcının seçtiği bildirim sıklığı. "Bugün uygulamaya girmedin" türü geri çağırma yoktur.
/// Günlük hatırlatma, push kanalı eklendiğinde gelecek.
/// </summary>
public enum NotificationPreference
{
    Off = 0,
    WeeklySummary = 1
}
