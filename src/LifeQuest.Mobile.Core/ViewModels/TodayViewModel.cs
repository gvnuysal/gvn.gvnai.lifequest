using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

/// <summary>Bugün ekranı (web: features/today).</summary>
public sealed partial class TodayViewModel(
    LifeQuestApi api, ProfileState profile, INavigator navigator, ReminderService reminders, TimeProvider clock) : ViewModelBase
{
    /// <summary>Görev günü 04:00'te başlar (sunucudaki Quests:DayStartHour ile aynı).</summary>
    public const string DayStartHour = "04";

    public ObservableCollection<QuestItem> OpenQuests { get; } = [];
    public ObservableCollection<QuestItem> CompletedToday { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateLabel), nameof(HasWeather), nameof(WeatherIcon), nameof(WeatherText), nameof(WeatherAdvice), nameof(ServerMessage))]
    public partial QuestList? Today { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelShort), nameof(LevelProgress), nameof(XpText))]
    public partial Progress? Progress { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSummary))]
    public partial WeeklySummary? Summary { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveStrip), nameof(HasActive))]
    public partial int ActiveCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SavedStrip), nameof(HasSaved))]
    public partial int SavedCount { get; set; }

    public string Greeting => $"{Format.Greeting(clock.GetLocalNow().DateTime)}, {profile.Current?.DisplayName.Split(' ')[0] ?? ""}".TrimEnd(',', ' ');
    public string DateLabel => Format.LongDate(Today is { } t ? t.Date.ToDateTime(TimeOnly.MinValue) : clock.GetLocalNow().DateTime);

    public string LevelShort => Progress is { } p ? S.Today.LevelShort(p.LifeLevel) : "";
    public double LevelProgress => Progress?.LevelProgress ?? 0;
    public string XpText => Progress is { } p ? $"{p.LifeXp} / {p.NextLevelXp} XP" : "";

    public bool HasWeather => Today?.Weather is not null;
    public string WeatherIcon => Today?.Weather is { } w ? Labels.WeatherIcon(w.Code) : "sun";
    public string WeatherText => Today?.Weather is { } w ? $"{w.City} · {Math.Round(w.TemperatureC):0}° · {w.Summary}" : "";
    public string? WeatherAdvice => Today?.Weather?.Advice;
    public string? ServerMessage => Today?.Message;

    public bool HasSummary => Summary is not null;
    public bool HasActive => ActiveCount > 0;
    public bool HasSaved => SavedCount > 0;
    public string ActiveStrip => S.Today.ActiveStrip(ActiveCount);
    public string SavedStrip => S.Today.SavedStrip(SavedCount);

    public bool HasOpenQuests => OpenQuests.Count > 0;
    public bool HasCompletedToday => CompletedToday.Count > 0;

    /// <summary>Tüm öneriler kapandı ve en az biri tamamlandı: "bugünü bitirdin".</summary>
    public bool AllDone => IsLoaded && OpenQuests.Count == 0 && CompletedToday.Count > 0;

    /// <summary>Hiç öneri yok (ör. profil çok kısıtlı).</summary>
    public bool NoSuggestions => IsLoaded && OpenQuests.Count == 0 && CompletedToday.Count == 0;

    public string AllDoneHint => S.Today.AllDoneHint(DayStartHour);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(async () =>
        {
            var today = api.TodayAsync();
            var active = api.ActiveAsync();
            var progress = api.ProgressAsync();
            await profile.LoadAsync();
            Apply(await today, (await active).Count, await progress);
        });

        // İkincil bilgiler: yüklenemezlerse sessizce gösterilmez.
        try { Summary = await api.LatestSummaryAsync(); } catch (ApiException) { }
        try { SavedCount = (await api.SavedAsync()).Count; } catch (ApiException) { }
        await reminders.RescheduleAsync(completedToday: CompletedToday.Count > 0);
    }

    private void Apply(QuestList today, int activeCount, Progress progress)
    {
        Today = today;
        ActiveCount = activeCount;
        Progress = progress;
        OpenQuests.Clear();
        CompletedToday.Clear();
        foreach (var q in today.Quests)
        {
            if (q.Status is QuestStatus.Offered or QuestStatus.Accepted) OpenQuests.Add(new QuestItem(q));
            else if (q.Status == QuestStatus.Completed) CompletedToday.Add(new QuestItem(q));
        }

        OnPropertyChanged(nameof(Greeting));
        OnPropertyChanged(nameof(HasOpenQuests));
        OnPropertyChanged(nameof(HasCompletedToday));
        OnPropertyChanged(nameof(AllDone));
        OnPropertyChanged(nameof(NoSuggestions));
    }

    [RelayCommand]
    private async Task DismissSummaryAsync()
    {
        if (Summary is not { } summary) return;
        Summary = null;
        try { await api.MarkSummaryReadAsync(summary.Id); } catch (ApiException) { }
    }

    [RelayCommand]
    private Task OpenQuestAsync(QuestItem item) => navigator.GoToAsync(Routes.Quest, new Dictionary<string, object> { ["id"] = item.Id });

    [RelayCommand]
    private Task GoSuggestAsync() => navigator.GoToAsync(Routes.Suggest);

    [RelayCommand]
    private Task GoActiveAsync() => navigator.GoToAsync(Routes.Quests);

    [RelayCommand]
    private Task GoSavedAsync() => navigator.GoToAsync(Routes.Saved);

    [RelayCommand]
    private Task GoProgressAsync() => navigator.GoToAsync(Routes.Progress);

    [RelayCommand]
    private Task GoProfileAsync() => navigator.GoToAsync(Routes.Profile);

    protected internal override void OnLanguageChanged()
    {
        // Görev metinleri sunucudan dile göre gelir: yeniden yüklenir.
        if (IsLoaded) _ = RefreshAsync();
        OnPropertyChanged(string.Empty);
    }
}
