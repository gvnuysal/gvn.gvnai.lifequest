using System.Globalization;
using LifeQuest.Mobile.Core.Localization;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile;

public partial class App : Application
{
    private readonly AppFlow _flow;
    private readonly IThemeService _theme;

    public App(AppFlow flow, IThemeService theme, ReminderService reminders)
    {
        InitializeComponent();
        _flow = flow;
        _theme = theme;
        // Hatırlatma metni kurulduğu dilde saklanır: dil değişince yeniden kurulur.
        Lang.Changed += (_, _) => _ = reminders.RescheduleAsync(completedToday: false);
        // Kodla boyanan bileşenler (seçim kutuları, ikonlar, halkalar) temayı oluşturulurken okur: tema değişince
        // ekran yeni temayla yeniden kurulur.
        RequestedThemeChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RebuildForTheme);
    }

    private void RebuildForTheme()
    {
        if (Windows.FirstOrDefault() is not { Page: { } page } window
            || IPlatformApplication.Current?.Services is not { } services)
            return;

        if (page is AppShell shell)
        {
            var tab = shell.CurrentState?.Location.OriginalString.TrimStart('/').Split('/').FirstOrDefault();
            var fresh = services.GetRequiredService<AppShell>();
            window.Page = fresh;
            if (!string.IsNullOrEmpty(tab)) _ = fresh.GoToAsync("//" + tab);
            return;
        }

        if (services.GetService(page.GetType()) is Page replacement)
            window.Page = replacement;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _theme.Apply(_theme.Current);
        var window = new Window(new ContentPage { Content = new ActivityIndicator { IsRunning = true, VerticalOptions = LayoutOptions.Center } });
        window.Created += async (_, _) => await _flow.StartAsync(CultureInfo.CurrentUICulture.Name);
        return window;
    }

    /// <summary>lifequest:// veya https://…/party/{kod} bağlantısı; oturum yoksa girişten sonra açılır.</summary>
    public static void OpenLink(Uri uri)
    {
        if (IPlatformApplication.Current?.Services.GetService<AppFlow>() is { } flow)
            MainThread.BeginInvokeOnMainThread(() => _ = flow.OpenLinkAsync(uri));
    }
}
