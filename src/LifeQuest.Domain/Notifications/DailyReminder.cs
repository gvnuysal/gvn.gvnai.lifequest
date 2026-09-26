using LifeQuest.Domain.Localization;

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
            return (Text.Of("Bugün bir adım?", "A step today?"), activeQuests == 1
                ? Text.Of("Aktif bir görevin var. Bugün tamamlamaya ne dersin?", "You have an active quest. How about finishing it today?")
                : Text.Of($"{activeQuests} aktif görevin var. Birini bugün bitirmeye ne dersin?", $"You have {activeQuests} active quests. How about finishing one today?"));

        return openOffers > 0
            ? (Text.Of("Bugünün görevleri hazır", "Today's quests are ready"), Text.Of($"Senin için {openOffers} öneri var. Hangisini seçeceksin?", $"There are {openOffers} suggestions for you. Which one will you pick?"))
            : (Text.Of("Bugünün görevleri hazır", "Today's quests are ready"), Text.Of("Yeni öneriler için LifeQuest'e göz at.", "Check LifeQuest for new suggestions."));
    }
}
