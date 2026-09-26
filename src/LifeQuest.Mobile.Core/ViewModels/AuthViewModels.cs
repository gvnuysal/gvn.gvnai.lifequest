using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Localization;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

public static partial class Validation
{
    public static bool IsEmail(string value) => EmailPattern().IsMatch(value.Trim());

    /// <summary>En az 8 karakter; harf ve rakam (web ile aynı kural).</summary>
    public static bool IsStrongPassword(string value)
        => value.Length >= 8 && value.Any(char.IsLetter) && value.Any(char.IsDigit);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}

public sealed partial class LoginViewModel(AuthService auth, AppFlow flow, INavigator navigator) : ViewModelBase
{
    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    public partial string? Error { get; set; }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!Validation.IsEmail(Email) || Password.Length == 0)
        {
            Error = S.Auth.EnterCredentials;
            return;
        }

        Error = null;
        IsBusy = true;
        try
        {
            await auth.LoginAsync(Email, Password);
            Password = "";
            await flow.EnterAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoRegisterAsync() => navigator.ShowRootAsync(AppRoot.Register);
}

public sealed partial class RegisterViewModel(AuthService auth, AppFlow flow, INavigator navigator, TimeProvider clock) : ViewModelBase
{
    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    public partial string BirthYear { get; set; } = (clock.GetLocalNow().Year - 25).ToString();

    [ObservableProperty]
    public partial string? DisplayNameError { get; set; }

    [ObservableProperty]
    public partial string? EmailError { get; set; }

    [ObservableProperty]
    public partial string? PasswordError { get; set; }

    [ObservableProperty]
    public partial string? BirthYearError { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var year = int.TryParse(BirthYear, out var y) ? y : 0;
        var name = DisplayName.Trim();
        DisplayNameError = name.Length is >= 2 and <= 50 ? null : S.Auth.DisplayNameError;
        EmailError = Validation.IsEmail(Email) ? null : S.Auth.EmailError;
        PasswordError = Validation.IsStrongPassword(Password) ? null : S.Auth.PasswordRule;
        BirthYearError = year >= 1900 && year <= clock.GetLocalNow().Year ? null : S.Auth.BirthYearHint;
        Error = null;
        if (DisplayNameError is not null || EmailError is not null || PasswordError is not null || BirthYearError is not null)
            return;

        IsBusy = true;
        try
        {
            await auth.RegisterAsync(new RegisterRequest(Email.Trim(), Password, name, year, Lang.Code));
            Password = "";
            await flow.EnterAsync();
        }
        catch (ApiException ex)
        {
            var fields = ex.FieldErrors();
            DisplayNameError = fields.GetValueOrDefault("displayName");
            EmailError = fields.GetValueOrDefault("email");
            PasswordError = fields.GetValueOrDefault("password");
            BirthYearError = fields.GetValueOrDefault("birthYear");
            if (fields.Count == 0) Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoLoginAsync() => navigator.ShowRootAsync(AppRoot.Login);
}

/// <summary>Giriş/kayıt ekranındaki ve profildeki TR / EN anahtarı.</summary>
public sealed partial class LanguageSwitchViewModel(ProfileState profile) : ViewModelBase
{
    public bool IsTurkish => Lang.Current == AppLanguage.Tr;
    public bool IsEnglish => Lang.Current == AppLanguage.En;

    /// <summary>Giriş yapılmışsa seçim hesaba da yazılır (push ve özet dili).</summary>
    public Func<AppLanguage, Task>? Persist { get; set; }

    [RelayCommand]
    private async Task SelectAsync(string code)
    {
        if (!Lang.TryParse(code, out var language) || language == Lang.Current)
            return;
        // Giriş yapılmışsa önce hesaba yazılır; sunucudan dönen profil dili uygular. Aksi halde yeniden yüklenen
        // profil henüz eski dili taşıyıp seçimi geri alabilirdi.
        if (Persist is not null) await Persist(language);
        else profile.ApplyLanguage(language);
    }

    protected internal override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(IsTurkish));
        OnPropertyChanged(nameof(IsEnglish));
    }
}
