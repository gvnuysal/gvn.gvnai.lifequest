using System.Reflection;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using AppTheme = LifeQuest.Mobile.Core.Services.AppTheme;
using IAppInfo = LifeQuest.Mobile.Core.Services.IAppInfo;
using IShare = LifeQuest.Mobile.Core.Services.IShare;
using IToast = LifeQuest.Mobile.Core.Services.IToast;

namespace LifeQuest.Mobile.Services;

/// <summary>iOS Keychain / Android Keystore.</summary>
public sealed class MauiSecureStore : ISecureStore
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
    public void Remove(string key) => SecureStorage.Default.Remove(key);
}

public sealed class MauiPreferences : IAppPreferences
{
    public string? Get(string key) => Preferences.Default.Get<string?>(key, null);

    public void Set(string key, string? value)
    {
        if (value is null) Preferences.Default.Remove(key);
        else Preferences.Default.Set(key, value);
    }
}

public sealed class MauiToast : IToast
{
    public void Show(string message) => Display(message);
    public void Success(string message) => Display(message);
    public void Error(string message) => Display(message, ToastDuration.Long);

    private static void Display(string message, ToastDuration duration = ToastDuration.Short)
        => MainThread.BeginInvokeOnMainThread(() => _ = Toast.Make(message, duration).Show());
}

public sealed class MauiDialogs : IDialogs
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
}

public sealed class MauiShare : IShare
{
    public Task ShareTextAsync(string title, string text)
        => Share.Default.RequestAsync(new ShareTextRequest { Title = title, Text = text });

    public async Task ShareFileAsync(string title, DownloadedFile file)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, file.FileName);
        await File.WriteAllBytesAsync(path, file.Content);
        await Share.Default.RequestAsync(new ShareFileRequest { Title = title, File = new ShareFile(path, file.ContentType) });
    }

    public Task OpenBrowserAsync(string url) => Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
}

public sealed class MauiThemeService(IAppPreferences preferences) : IThemeService
{
    private const string Key = "lq.theme";

    public AppTheme Current => Enum.TryParse<AppTheme>(preferences.Get(Key), out var theme) ? theme : AppTheme.System;

    public void Apply(AppTheme theme)
    {
        preferences.Set(Key, theme == AppTheme.System ? null : theme.ToString());
        if (Application.Current is { } app)
        {
            app.UserAppTheme = theme switch
            {
                AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
                AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
                _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
            };
        }
    }
}

/// <summary>Plugin.LocalNotification: iOS UNUserNotificationCenter, Android AlarmManager.</summary>
public sealed class MauiLocalNotifications : ILocalNotifications
{
    public async Task<bool> RequestPermissionAsync()
    {
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled())
            return true;
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public Task CancelAllAsync()
    {
        LocalNotificationCenter.Current.CancelAll();
        return Task.CompletedTask;
    }

    public Task ScheduleAsync(int id, DateTime localTime, string title, string body)
        => LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = id,
            Title = title,
            Description = body,
            Schedule = new NotificationRequestSchedule { NotifyTime = localTime }
        });
}

/// <summary>Derleme ayarları: API ve web adresleri (csproj'taki LifeQuestApiBaseUrl / LifeQuestWebBaseUrl).</summary>
public sealed class MauiAppInfo : IAppInfo
{
    private const string ServerKey = "lq.server";

    public string WebBaseUrl { get; } = Metadata("LifeQuestWebBaseUrl") ?? "https://lifequesttest.gvnaitech.com";

    public string TimeZoneId => TimeZoneInfo.Local.Id;

    /// <summary>
    /// API adresi: geliştirme sürümünde uygulama içinden değiştirilebilir; yoksa derleme ayarı; o da yoksa
    /// yerel geliştirme sunucusu (Android emülatöründen bilgisayar 10.0.2.2'dir).
    /// </summary>
    public static Uri ApiBaseUrl(IAppPreferences preferences)
    {
#if DEBUG
        if (Uri.TryCreate(preferences.Get(ServerKey), UriKind.Absolute, out var custom))
            return custom;
#endif
        if (Uri.TryCreate(Metadata("LifeQuestApiBaseUrl"), UriKind.Absolute, out var configured))
            return configured;
        return new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080");
    }

    public static void SetServer(IAppPreferences preferences, string? url) => preferences.Set(ServerKey, url);

    private static string? Metadata(string key) => typeof(MauiAppInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == key)?.Value is { Length: > 0 } value ? value : null;
}
