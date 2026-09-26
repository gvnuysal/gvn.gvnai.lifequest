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
