namespace LifeQuest.Mobile.Core.Localization;

/// <summary>Yalnızca mobilde olan metinler (web sözlüğünde karşılığı yok).</summary>
public sealed class MobileStrings : LocalizedStrings
{
    public static MobileStrings Instance { get; } = new();

    // Günlük hatırlatma (sunucudaki DailyReminder metinleriyle aynı ton).
    public string ReminderTitle => T("Bugün bir adım?", "A step today?");
    public string ReminderBody => T("Bugünün görevleri seni bekliyor. Hangisini seçeceksin?", "Today's quests are waiting. Which one will you pick?");
    public string ReminderDenied => T(
        "Bildirim izni kapalı. Telefonun ayarlarından LifeQuest için bildirimlere izin verip tekrar dene.",
        "Notification permission is off. Allow notifications for LifeQuest in your phone's settings and try again.");
    public string ReminderLocalHint => T(
        "Hatırlatma bu cihazda zamanlanır; o gün görev tamamladıysan gelmez.",
        "The reminder is scheduled on this device; it won't come if you've completed a quest that day.");

    // Geliştirme: sunucu adresi.
    public string ServerAddress => T("Sunucu adresi", "Server address");
    public string ServerAddressHint => T("Yalnızca geliştirme sürümünde görünür.", "Only visible in development builds.");

    // Paylaşım ve dosyalar.
    public string ShareInvite => T("Daveti paylaş", "Share invite");
    public string ExportTitle => T("LifeQuest verilerim", "My LifeQuest data");
    public string CalendarTitle => T("Takvime ekle", "Add to calendar");

    public string Retry => T("Tekrar dene", "Try again");
    public string Offline => T("Çevrimdışısın. Bağlantı gelince tekrar dene.", "You're offline. Try again when you're connected.");
}
