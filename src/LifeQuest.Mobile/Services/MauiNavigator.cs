using LifeQuest.Mobile.Core.Services;
using LifeQuest.Mobile.Views;

namespace LifeQuest.Mobile.Services;

/// <summary>
/// Kök akış (giriş, kayıt, onboarding, ana sekmeler) pencerenin sayfası değiştirilerek; ana akış içi gezinme Shell
/// rotalarıyla yapılır.
/// </summary>
public sealed class MauiNavigator(IServiceProvider services) : INavigator
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        if (Shell.Current is null) return;
        if (parameters is null) await Shell.Current.GoToAsync(route);
        else await Shell.Current.GoToAsync(route, new ShellNavigationQueryParameters(parameters));
    });

    public Task BackAsync() => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        if (Shell.Current?.Navigation.NavigationStack.Count > 1) await Shell.Current.GoToAsync("..");
        else if (Shell.Current is not null) await Shell.Current.GoToAsync(Routes.Today);
    });

    public Task ShowRootAsync(AppRoot root) => MainThread.InvokeOnMainThreadAsync(() =>
    {
        if (Application.Current?.Windows.FirstOrDefault() is not { } window) return;
        window.Page = root switch
        {
            AppRoot.Login => services.GetRequiredService<LoginPage>(),
            AppRoot.Register => services.GetRequiredService<RegisterPage>(),
            AppRoot.Onboarding => services.GetRequiredService<OnboardingPage>(),
            _ => services.GetRequiredService<AppShell>()
        };
    });
}
