using CommunityToolkit.Maui;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Services;
using LifeQuest.Mobile.Core.ViewModels;
using LifeQuest.Mobile.Services;
using LifeQuest.Mobile.Views;
using Plugin.LocalNotification;
using IAppInfo = LifeQuest.Mobile.Core.Services.IAppInfo;
using IShare = LifeQuest.Mobile.Core.Services.IShare;
using IToast = LifeQuest.Mobile.Core.Services.IToast;

namespace LifeQuest.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if DEBUG
        // Geliştirmede yakalanmayan istisnalar konsola yazılır (simülatör/emülatör günlüğünde görünür).
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Console.WriteLine($"[LifeQuest] Unhandled: {e.ExceptionObject}");
#if IOS
        ObjCRuntime.Runtime.MarshalManagedException += (_, e) => Console.WriteLine($"[LifeQuest] Managed: {e.Exception}");
#endif
#endif
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Nunito-Regular.ttf", "NunitoRegular");
                fonts.AddFont("Nunito-SemiBold.ttf", "NunitoSemiBold");
                fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtraBold");
                fonts.AddFont("Nunito-Black.ttf", "NunitoBlack");
            });

        // Girdi kutuları kendi kenarlığımızın (Border) içinde: platformun çerçevesi ve alt çizgisi kaldırılır.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("LifeQuestPlain", (handler, _) =>
        {
#if IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("LifeQuestPlain", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("LifeQuestPlain", (handler, _) =>
        {
#if IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });
        Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping("LifeQuestPlain", (handler, _) =>
        {
#if IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });

        var services = builder.Services;

        // Platform hizmetleri
        services.AddSingleton<IAppPreferences, MauiPreferences>();
        services.AddSingleton<ISecureStore, MauiSecureStore>();
        services.AddSingleton<INavigator, MauiNavigator>();
        services.AddSingleton<IToast, MauiToast>();
        services.AddSingleton<IDialogs, MauiDialogs>();
        services.AddSingleton<IShare, MauiShare>();
        services.AddSingleton<IThemeService, MauiThemeService>();
        services.AddSingleton<ILocalNotifications, MauiLocalNotifications>();
        services.AddSingleton<IAppInfo, MauiAppInfo>();
        services.AddSingleton(TimeProvider.System);

        // API ve oturum: platformun yerel HTTP katmanı (NSUrlSession / AndroidMessageHandler).
        services.AddSingleton(sp =>
        {
            var (api, session, auth) = ApiClientFactory.Create(
                MauiAppInfo.ApiBaseUrl(sp.GetRequiredService<IAppPreferences>()),
                sp.GetRequiredService<ISecureStore>(),
                () => new HttpClientHandler { UseCookies = false });
            return new ApiClients(api, session, auth);
        });
        services.AddSingleton(sp => sp.GetRequiredService<ApiClients>().Api);
        services.AddSingleton(sp => sp.GetRequiredService<ApiClients>().Session);
        services.AddSingleton(sp => sp.GetRequiredService<ApiClients>().Auth);

        // Uygulama durumu
        services.AddSingleton<ProfileState>();
        services.AddSingleton<AppFlow>();
        services.AddSingleton<ReminderService>();

        // Ekranlar
        services.AddTransient<LoginViewModel>().AddTransient<LoginPage>();
        services.AddTransient<RegisterViewModel>().AddTransient<RegisterPage>();
        services.AddTransient<OnboardingViewModel>().AddTransient<OnboardingPage>();
        services.AddTransient<TodayViewModel>().AddTransient<TodayPage>();
        services.AddTransient<SuggestViewModel>().AddTransient<SuggestPage>();
        services.AddTransient<QuestViewModel>().AddTransient<QuestPage>();
        services.AddTransient<MyQuestsViewModel>().AddTransient<MyQuestsPage>();
        services.AddTransient<ProgressViewModel>().AddTransient<ProgressPage>();
        services.AddTransient<SavedViewModel>().AddTransient<SavedPage>();
        services.AddTransient<IdeasViewModel>().AddTransient<IdeasPage>();
        services.AddTransient<PartyViewModel>().AddTransient<PartyPage>();
        services.AddTransient<ProfileViewModel>().AddTransient<ProfilePage>();
        services.AddTransient<LanguageSwitchViewModel>();
        services.AddTransient<AppShell>();

        return builder.Build();
    }
}

internal sealed record ApiClients(LifeQuestApi Api, SessionStore Session, AuthService Auth);
