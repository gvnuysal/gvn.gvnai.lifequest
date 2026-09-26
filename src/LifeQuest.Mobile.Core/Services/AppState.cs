using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.Services;

/// <summary>Oturumdaki kullanıcının profili; ekranlar arasında paylaşılır. Profildeki dil uygulamaya uygulanır.</summary>
public sealed class ProfileState(LifeQuestApi api, IAppPreferences preferences)
{
    public const string LanguageKey = "lq.lang";

    public Profile? Current { get; private set; }

    public event EventHandler? Changed;

    public async Task<Profile> LoadAsync(bool force = false)
    {
        if (Current is not null && !force)
            return Current;
        Set(await api.ProfileAsync());
        return Current!;
    }

    public void Set(Profile profile)
    {
        Current = profile;
        if (Lang.TryParse(profile.Language, out var language))
            ApplyLanguage(language);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear() => Current = null;

    /// <summary>Dil cihazda saklanır; bir sonraki açılışta girişten önce de bu dil kullanılır.</summary>
    public void ApplyLanguage(AppLanguage language)
    {
        preferences.Set(LanguageKey, Lang.ToCode(language));
        Lang.Set(language);
    }
}

/// <summary>
/// Açılış ve oturum akışı: dil, oturumun geri yüklenmesi, onboarding kontrolü; oturum bitince giriş ekranı.
/// </summary>
public sealed class AppFlow
{
    private readonly SessionStore _session;
    private readonly ProfileState _profile;
    private readonly INavigator _navigator;
    private readonly IToast _toast;
    private readonly IAppPreferences _preferences;

    public AppFlow(SessionStore session, ProfileState profile, INavigator navigator, IToast toast, IAppPreferences preferences)
    {
        (_session, _profile, _navigator, _toast, _preferences) = (session, profile, navigator, toast, preferences);
        _session.Ended += (_, reason) => _ = OnEndedAsync(reason);
    }

    /// <summary>Derin bağlantıdan gelen ve oturum açıldıktan sonra gidilecek rota (ör. parti daveti).</summary>
    public (string Route, IDictionary<string, object> Parameters)? Pending { get; set; }

    public async Task StartAsync(string deviceCulture)
    {
        Lang.Set(Lang.Detect(_preferences.Get(ProfileState.LanguageKey), deviceCulture));

        bool restored;
        try
        {
            restored = await _session.RestoreAsync();
        }
        catch (ApiException ex)
        {
            // Çevrimdışı açılış: oturum korunur, kullanıcı giriş ekranında tekrar dener.
            _toast.Error(ex.Message);
            restored = false;
        }

        if (!restored)
        {
            await _navigator.ShowRootAsync(AppRoot.Login);
            return;
        }

        await EnterAsync();
    }

    /// <summary>Giriş/kayıt sonrası: profil yüklenir, onboarding bitmemişse oraya gidilir.</summary>
    /// <param name="reloadProfile">Onboarding az önce kaydedilen profili verdiyse yeniden yüklenmez.</param>
    public async Task EnterAsync(bool reloadProfile = true)
    {
        try
        {
            var profile = await _profile.LoadAsync(force: reloadProfile);
            if (!profile.OnboardingCompleted)
            {
                await _navigator.ShowRootAsync(AppRoot.Onboarding);
                return;
            }
        }
        catch (ApiException ex)
        {
            _toast.Error(ex.Message);
        }

        await _navigator.ShowRootAsync(AppRoot.Main);
        await OpenPendingAsync();
    }

    public async Task OpenPendingAsync()
    {
        if (Pending is not { } pending || !_session.HasSession)
            return;
        Pending = null;
        await _navigator.GoToAsync(pending.Route, pending.Parameters);
    }

    /// <summary>lifequest://party/{kod} veya https://…/party/{kod} bağlantısı.</summary>
    public async Task OpenLinkAsync(Uri uri)
    {
        var segments = (uri.Scheme == "lifequest" ? uri.Host + uri.AbsolutePath : uri.AbsolutePath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments is not ["party", var code])
            return;

        Pending = (Routes.Party, new Dictionary<string, object> { ["code"] = Uri.UnescapeDataString(code) });
        if (_session.HasSession && _profile.Current?.OnboardingCompleted == true)
            await OpenPendingAsync();
    }

    private async Task OnEndedAsync(SessionEndReason reason)
    {
        _profile.Clear();
        var s = Localizer.Instance.S.Errors;
        if (reason == SessionEndReason.Suspended) _toast.Error(s.Suspended);
        else if (reason == SessionEndReason.Expired) _toast.Error(s.SessionExpired);
        await _navigator.ShowRootAsync(AppRoot.Login);
    }
}
