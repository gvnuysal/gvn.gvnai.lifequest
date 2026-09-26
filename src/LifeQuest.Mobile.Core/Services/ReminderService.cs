using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.Services;

/// <summary>
/// Cihazda zamanlanan günlük hatırlatma. Önümüzdeki 7 gün için ayrı bildirimler kurulur; uygulama açıldıkça ve
/// görev tamamlandıkça yeniden kurulur. O gün görev tamamlandıysa o günün bildirimi atlanır (sunucudaki web push
/// hatırlatmasıyla aynı kural). Ayar yalnızca bu cihazdadır.
/// </summary>
public sealed class ReminderService(ILocalNotifications notifications, IAppPreferences preferences, TimeProvider clock)
{
    public const string HourKey = "lq.reminder.hour";
    public const int Days = 7;
    public const int MinHour = 7;
    public const int MaxHour = 22;

    public int? Hour => int.TryParse(preferences.Get(HourKey), out var h) && h is >= MinHour and <= MaxHour ? h : null;

    /// <summary>İzin istenir; verilmezse false döner ve ayar değişmez.</summary>
    public async Task<bool> EnableAsync(int hour, bool completedToday)
    {
        if (hour is < MinHour or > MaxHour) throw new ArgumentOutOfRangeException(nameof(hour));
        if (!await notifications.RequestPermissionAsync())
            return false;

        preferences.Set(HourKey, hour.ToString());
        await RescheduleAsync(completedToday);
        return true;
    }

    public async Task DisableAsync()
    {
        preferences.Set(HourKey, null);
        await notifications.CancelAllAsync();
    }

    public async Task RescheduleAsync(bool completedToday)
    {
        await notifications.CancelAllAsync();
        if (Hour is not { } hour)
            return;

        var now = clock.GetLocalNow().DateTime;
        var s = MobileStrings.Instance;
        for (var day = 0; day < Days; day++)
        {
            if (day == 0 && completedToday) continue;
            var at = now.Date.AddDays(day).AddHours(hour);
            if (at <= now) continue;
            await notifications.ScheduleAsync(1000 + day, at, s.ReminderTitle, s.ReminderBody);
        }
    }
}
