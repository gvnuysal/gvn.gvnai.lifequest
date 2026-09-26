using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Localization;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

public sealed record ProfileInterestItem(string Name, string ColorKey, bool Learned, double Weight)
{
    public string Percent => Format.Percent(Weight);
}

/// <summary>Profil (web: features/profile): tercihler, ilgi alanları, dil ve tema, hatırlatma, hesap.</summary>
public sealed partial class ProfileViewModel(
    LifeQuestApi api,
    ProfileState profile,
    AuthService auth,
    SessionStore session,
    ReminderService reminders,
    IThemeService theme,
    INavigator navigator,
    IToast toast,
    IShare share) : ViewModelBase
{
    public ObservableCollection<Choice<DiscoveryRadius>> Radii { get; } = Choices.Radii();
    public ObservableCollection<Choice<CostBand>> Budgets { get; } = Choices.Costs();
    public ObservableCollection<Choice<int>> WeeklyTimes { get; } =
        [.. Labels.WeeklyTimeOptions.Select(m => new Choice<int>(m, () => Labels.WeeklyTime(m).Short))];
    public ObservableCollection<Choice<PhysicalEffort>> Efforts { get; } = Choices.EffortLimits();
    public ObservableCollection<Choice<NotificationPreference>> Notifications { get; } =
    [
        new(NotificationPreference.WeeklySummary, () => Labels.Notification(NotificationPreference.WeeklySummary)),
        new(NotificationPreference.Off, () => Labels.Notification(NotificationPreference.Off))
    ];
    public ObservableCollection<Choice<LifeCategory>> Goals { get; } = Choices.Categories(withDescription: false);
    public ObservableCollection<Choice<AppTheme>> Themes { get; } =
    [
        new(AppTheme.System, () => S.Profile.Themes.System),
        new(AppTheme.Light, () => S.Profile.Themes.Light),
        new(AppTheme.Dark, () => S.Profile.Themes.Dark)
    ];
    public ObservableCollection<ProfileInterestItem> Interests { get; } = [];
    public ObservableCollection<InterestGroup> InterestDraft { get; } = [];
    public ObservableCollection<Choice<int>> ReminderHours { get; } =
        [.. Enumerable.Range(ReminderService.MinHour, ReminderService.MaxHour - ReminderService.MinHour + 1).Select(h => new Choice<int>(h, () => Format.Time(h)))];

    public LanguageSwitchViewModel Language { get; } = new(profile);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Dirty))]
    public partial string City { get; set; } = "";

    [ObservableProperty]
    public partial bool InterestsOpen { get; set; }

    [ObservableProperty]
    public partial bool DeleteOpen { get; set; }

    [ObservableProperty]
    public partial string DeletePassword { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReminderText), nameof(ReminderOn))]
    public partial int? ReminderHour { get; set; }

    public string DisplayName => profile.Current?.DisplayName ?? "";
    public string Email => profile.Current?.Email ?? "";
    public string Initial => DisplayName.Length > 0 ? DisplayName[..1].ToUpper(Lang.Culture) : "?";
    public string RadiusDescription => Radii.FirstOrDefault(r => r.IsSelected) is { } r ? Labels.Radius(r.Value).Description : "";

    public bool ReminderOn => ReminderHour is not null;
    public string ReminderText => ReminderHour is { } h ? S.Reminder.Active(Format.Time(h)) : S.Reminder.Off;

    /// <summary>Formdaki tercihler profilden farklı mı (kaydet düğmesi).</summary>
    public bool Dirty
    {
        get
        {
            if (profile.Current is not { } p || !IsLoaded) return false;
            if (!Radii.Any(c => c.IsSelected) || !Budgets.Any(c => c.IsSelected) || !WeeklyTimes.Any(c => c.IsSelected)
                || !Efforts.Any(c => c.IsSelected) || !Notifications.Any(c => c.IsSelected))
                return false;
            return p.DiscoveryRadius != Selected(Radii)
                   || p.Budget != Selected(Budgets)
                   || p.WeeklyAvailableMinutes != Selected(WeeklyTimes)
                   || (p.City ?? "") != City.Trim()
                   || p.MaxPhysicalEffort != Selected(Efforts)
                   || p.NotificationPreference != Selected(Notifications)
                   || !p.Goals.Order().SequenceEqual(Goals.Where(g => g.IsSelected).Select(g => g.Value).Order());
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Language.Persist = SaveLanguageAsync;
        Choices.Select(Themes, theme.Current);
        ReminderHour = reminders.Hour;
        Choices.Select(ReminderHours, ReminderHour ?? 19);
        await LoadAsync(async () => Reset(await profile.LoadAsync(force: true)));
    }

    [RelayCommand]
    private void Pick(object choice)
    {
        switch (choice)
        {
            case Choice<DiscoveryRadius> r: Choices.Select(Radii, r.Value); OnPropertyChanged(nameof(RadiusDescription)); break;
            case Choice<CostBand> b: Choices.Select(Budgets, b.Value); break;
            case Choice<int> w when WeeklyTimes.Contains(w): Choices.Select(WeeklyTimes, w.Value); break;
            case Choice<int> h when ReminderHours.Contains(h): Choices.Select(ReminderHours, h.Value); break;
            case Choice<PhysicalEffort> e: Choices.Select(Efforts, e.Value); break;
            case Choice<NotificationPreference> n: Choices.Select(Notifications, n.Value); break;
            case Choice<LifeCategory> g: g.IsSelected = !g.IsSelected; break;
            case Choice<AppTheme> t: Choices.Select(Themes, t.Value); theme.Apply(t.Value); break;
        }

        OnPropertyChanged(nameof(Dirty));
    }

    [RelayCommand]
    private Task SavePreferencesAsync() => RunAsync(async () =>
    {
        var city = City.Trim();
        profile.Set(await api.UpdatePreferencesAsync(new PreferencesRequest
        {
            DiscoveryRadius = Selected(Radii),
            Budget = Selected(Budgets),
            WeeklyAvailableMinutes = Selected(WeeklyTimes),
            Goals = Goals.Where(g => g.IsSelected).Select(g => g.Value).ToList(),
            City = city.Length > 0 ? city : null,
            ClearCity = city.Length == 0,
            MaxPhysicalEffort = Selected(Efforts),
            NotificationPreference = Selected(Notifications)
        }));
        Reset(profile.Current!);
        toast.Success(S.Profile.SavedPrefs);
    }, toast);

    [RelayCommand]
    private async Task OpenInterestsAsync()
    {
        if (profile.Current is not { } p) return;
        await RunAsync(async () =>
        {
            var weights = p.Interests.Where(i => i.Source == InterestSource.Explicit).ToDictionary(i => i.Code, i => i.Weight);
            InterestDraft.Clear();
            foreach (var group in InterestGroup.Build(await api.InterestsAsync(), weights)) InterestDraft.Add(group);
            InterestsOpen = true;
        }, toast);
    }

    [RelayCommand]
    private static void CycleInterest(InterestChoice interest) => interest.Cycle();

    [RelayCommand]
    private Task SaveInterestsAsync()
    {
        var selection = InterestDraft.SelectMany(g => g).Where(i => i.IsSelected).Select(i => new InterestSelection(i.Code, i.Weight)).ToList();
        if (selection.Count == 0)
        {
            toast.Error(S.Profile.PickInterest);
            return Task.CompletedTask;
        }

        return RunAsync(async () =>
        {
            profile.Set(await api.SetInterestsAsync(selection));
            Reset(profile.Current!);
            InterestsOpen = false;
            toast.Success(S.Profile.InterestsSaved);
        }, toast);
    }

    [RelayCommand]
    private Task EnableReminderAsync() => RunAsync(async () =>
    {
        var hour = ReminderHours.First(h => h.IsSelected).Value;
        if (!await reminders.EnableAsync(hour, completedToday: false))
        {
            toast.Error(MobileStrings.Instance.ReminderDenied);
            return;
        }

        ReminderHour = hour;
        toast.Success(S.Reminder.Enabled(Format.Time(hour)));
    }, toast);

    [RelayCommand]
    private Task DisableReminderAsync() => RunAsync(async () =>
    {
        await reminders.DisableAsync();
        ReminderHour = null;
        toast.Show(S.Reminder.Disabled);
    }, toast);

    [RelayCommand]
    private Task ExportAsync() => RunAsync(async () =>
    {
        var file = await api.ExportDataAsync();
        await share.ShareFileAsync(MobileStrings.Instance.ExportTitle, file with { FileName = S.Profile.ExportFile });
    }, toast);

    [RelayCommand]
    private Task GoSavedAsync() => navigator.GoToAsync(Routes.Saved);

    [RelayCommand]
    private Task GoIdeasAsync() => navigator.GoToAsync(Routes.Ideas);

    [RelayCommand]
    private Task LogoutAsync() => auth.LogoutAsync();

    [RelayCommand]
    private void OpenDelete()
    {
        DeletePassword = "";
        DeleteOpen = true;
    }

    [RelayCommand]
    private Task DeleteAccountAsync() => DeletePassword.Length == 0 ? Task.CompletedTask : RunAsync(async () =>
    {
        await api.DeleteAccountAsync(DeletePassword);
        DeleteOpen = false;
        DeletePassword = "";
        await reminders.DisableAsync();
        toast.Show(S.Profile.Deleted);
        await session.EndAsync(SessionEndReason.SignedOut);
        await navigator.ShowRootAsync(AppRoot.Register);
    }, toast);

    private async Task SaveLanguageAsync(AppLanguage language)
    {
        try
        {
            // profile.Set dili uygular (ve dil olayıyla ekran yeni dilde yeniden yüklenir).
            profile.Set(await api.UpdatePreferencesAsync(new PreferencesRequest { Language = Lang.ToCode(language) }));
            await reminders.RescheduleAsync(completedToday: false);
        }
        catch (ApiException ex)
        {
            toast.Error(ex.Message);
        }
    }

    private void Reset(Profile p)
    {
        Choices.Select(Radii, p.DiscoveryRadius);
        Choices.Select(Budgets, p.Budget);
        Choices.Select(WeeklyTimes, Labels.WeeklyTimeOptions.MinBy(o => Math.Abs(o - p.WeeklyAvailableMinutes)));
        Choices.Select(Efforts, p.MaxPhysicalEffort == PhysicalEffort.None ? PhysicalEffort.Light : p.MaxPhysicalEffort);
        Choices.Select(Notifications, p.NotificationPreference);
        foreach (var g in Goals) g.IsSelected = p.Goals.Contains(g.Value);
        City = p.City ?? "";

        Interests.Clear();
        foreach (var i in p.Interests.OrderByDescending(i => i.Weight))
            Interests.Add(new(i.Name, Labels.Category(i.Category).ColorKey, i.Source == InterestSource.Learned, i.Weight));

        OnPropertyChanged(string.Empty);
    }

    private static T Selected<T>(IEnumerable<Choice<T>> choices) => choices.First(c => c.IsSelected).Value;

    protected internal override void OnLanguageChanged()
    {
        Choices.Refresh(Radii);
        Choices.Refresh(Budgets);
        Choices.Refresh(WeeklyTimes);
        Choices.Refresh(Efforts);
        Choices.Refresh(Notifications);
        Choices.Refresh(Goals);
        Choices.Refresh(Themes);
        // İlgi adları sunucudan dile göre gelir.
        if (IsLoaded) _ = RefreshAsync();
        OnPropertyChanged(string.Empty);
    }
}
