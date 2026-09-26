using LifeQuest.Mobile.Core.Api;

namespace LifeQuest.Mobile.Core.Services;

// MAUI tarafının sağladığı platform hizmetleri. ViewModel'ler yalnızca bu arayüzleri bilir; testlerde sahteleri kullanılır.

/// <summary>Uygulama içi gezinme. Rotalar <see cref="Routes"/>'ta.</summary>
public interface INavigator
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task BackAsync();

    /// <summary>Kök akışı değiştirir: giriş, onboarding ya da ana sekmeler.</summary>
    Task ShowRootAsync(AppRoot root);
}

public enum AppRoot
{
    Login,
    Register,
    Onboarding,
    Main
}

public static class Routes
{
    public const string Today = "//today";
    public const string Quests = "//quests";
    public const string Progress = "//progress";
    public const string Profile = "//profile";
    public const string Quest = "quest";
    public const string Suggest = "suggest";
    public const string Saved = "saved";
    public const string Ideas = "ideas";
    public const string Party = "party";
    public const string Celebration = "celebration";
}

public interface IToast
{
    void Show(string message);
    void Success(string message);
    void Error(string message);
}

public interface IDialogs
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

public interface IShare
{
    Task ShareTextAsync(string title, string text);
    Task ShareFileAsync(string title, DownloadedFile file);
    Task OpenBrowserAsync(string url);
}

/// <summary>Cihazda saklanan küçük ayarlar (dil, tema, hatırlatma saati).</summary>
public interface IAppPreferences
{
    string? Get(string key);
    void Set(string key, string? value);
}

public enum AppTheme
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    AppTheme Current { get; }
    void Apply(AppTheme theme);
}

/// <summary>Yerel bildirim zamanlayıcısı (iOS UNUserNotificationCenter / Android AlarmManager).</summary>
public interface ILocalNotifications
{
    Task<bool> RequestPermissionAsync();
    Task CancelAllAsync();
    Task ScheduleAsync(int id, DateTime localTime, string title, string body);
}

/// <summary>Uygulama bilgileri: davet bağlantıları için web adresi, cihaz saat dilimi.</summary>
public interface IAppInfo
{
    /// <summary>Paylaşılan davet bağlantılarının kökü, ör. https://lifequesttest.gvnaitech.com</summary>
    string WebBaseUrl { get; }
    string TimeZoneId { get; }
}
