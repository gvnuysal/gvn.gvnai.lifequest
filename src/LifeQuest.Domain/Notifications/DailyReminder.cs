namespace LifeQuest.Domain.Notifications;

/// <summary>
/// Kullanıcının seçtiği yerel saatte, günde en fazla bir kez gönderilen push hatırlatması. Gece saatleri seçilemez;
/// o gün zaten görev tamamlamış kullanıcıya hatırlatma gitmez (geri çağırma değil, yardımcı dürtü).
/// </summary>
public static class DailyReminder
{
    public const int EarliestHour = 7;
    public const int LatestHour = 22;

    public static bool IsValidHour(int hour) => hour is >= EarliestHour and <= LatestHour;

    /// <summary>Yerel saat seçilen saatin içindeyse ve bu takvim gününde henüz gönderilmediyse.</summary>
    public static bool IsDue(int reminderHour, DateOnly? lastSentOn, DateTime localNow)
        => localNow.Hour == reminderHour && lastSentOn != DateOnly.FromDateTime(localNow);

    public static (string Title, string Body) Compose(int activeQuests, int openOffers)
    {
        if (activeQuests > 0)
            return ("Bugün bir adım?", activeQuests == 1
                ? "Aktif bir görevin var. Bugün tamamlamaya ne dersin?"
                : $"{activeQuests} aktif görevin var. Birini bugün bitirmeye ne dersin?");

        return openOffers > 0
            ? ("Bugünün görevleri hazır", $"Senin için {openOffers} öneri var. Hangisini seçeceksin?")
            : ("Bugünün görevleri hazır", "Yeni öneriler için LifeQuest'e göz at.");
    }
}
