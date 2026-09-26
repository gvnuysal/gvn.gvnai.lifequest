using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

/// <summary>Boş vakte göre öneri (web: features/suggest).</summary>
public sealed partial class SuggestViewModel(LifeQuestApi api, ProfileState profile, INavigator navigator, IToast toast) : ViewModelBase
{
    public ObservableCollection<Choice<int>> Durations { get; } =
    [
        .. new[] { 30, 60, 120, 180 }.Select(m => new Choice<int>(m, () => S.Suggest.Options[m.ToString()]))
    ];

    public ObservableCollection<Choice<CostBand>> Costs { get; } = Choices.Costs();
    public ObservableCollection<QuestItem> Results { get; } = [];

    [ObservableProperty]
    public partial bool LimitReached { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults), nameof(NoResults), nameof(ResultTitle), nameof(ServerMessage))]
    public partial QuestList? Result { get; set; }

    public bool HasResults => Results.Count > 0;
    public bool NoResults => Result is not null && Results.Count == 0;
    public string ResultTitle => S.Suggest.ForYou;
    public string? ServerMessage => Result?.Message;

    [RelayCommand]
    private void Init()
    {
        if (Durations.Any(d => d.IsSelected)) return;
        Choices.Select(Durations, 120);
        Choices.Select(Costs, profile.Current?.Budget ?? CostBand.Low);
    }

    [RelayCommand]
    private void PickDuration(Choice<int> choice) => Choices.Select(Durations, choice.Value);

    [RelayCommand]
    private void PickCost(Choice<CostBand> choice) => Choices.Select(Costs, choice.Value);

    [RelayCommand]
    private async Task SuggestAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LimitReached = false;
        try
        {
            var list = await api.SuggestAsync(new SuggestRequest(
                Durations.FirstOrDefault(d => d.IsSelected)?.Value, Costs.FirstOrDefault(c => c.IsSelected)?.Value));
            Results.Clear();
            foreach (var q in list.Quests) Results.Add(new QuestItem(q));
            Result = list;
        }
        catch (ApiException ex)
        {
            LimitReached = ex.Has("SUGGESTION_LIMIT_REACHED");
            if (!LimitReached) toast.Error(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenQuestAsync(QuestItem item) => navigator.GoToAsync(Routes.Quest, new Dictionary<string, object> { ["id"] = item.Id });

    [RelayCommand]
    private Task BackAsync() => navigator.BackAsync();

    protected internal override void OnLanguageChanged()
    {
        Choices.Refresh(Durations);
        Choices.Refresh(Costs);
        foreach (var r in Results) r.Refresh();
        OnPropertyChanged(string.Empty);
    }
}

/// <summary>Görevlerim: devam edenler ve sayfalı geçmiş (web: features/active).</summary>
public sealed partial class MyQuestsViewModel(LifeQuestApi api, INavigator navigator) : ViewModelBase
{
    public const int PageSize = 20;

    public ObservableCollection<QuestItem> Active { get; } = [];
    public ObservableCollection<HistoryItem> History { get; } = [];

    public ObservableCollection<Choice<QuestStatus?>> Filters { get; } =
    [
        new(null, () => S.Quests.FilterAll),
        new(QuestStatus.Completed, () => S.Quests.FilterCompleted),
        new(QuestStatus.Skipped, () => S.Quests.FilterSkipped),
        new(QuestStatus.Expired, () => S.Quests.FilterExpired)
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowActive), nameof(ShowHistory))]
    public partial bool HistoryTab { get; set; }

    [ObservableProperty]
    public partial bool HasMore { get; set; }

    [ObservableProperty]
    public partial bool LoadingHistory { get; set; }

    private int _page;
    private bool _historyLoaded;

    public bool ShowActive => !HistoryTab;
    public bool ShowHistory => HistoryTab;
    public bool NoActive => IsLoaded && Active.Count == 0;
    public bool NoHistory => _historyLoaded && !LoadingHistory && History.Count == 0;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(async () =>
        {
            var active = await api.ActiveAsync();
            Active.Clear();
            foreach (var q in active) Active.Add(new QuestItem(q));
        });
        OnPropertyChanged(nameof(NoActive));
        if (HistoryTab) await LoadHistoryAsync(1);
    }

    [RelayCommand]
    private async Task ShowTabAsync(string tab)
    {
        HistoryTab = tab == "history";
        if (HistoryTab && !_historyLoaded)
        {
            if (!Filters.Any(f => f.IsSelected)) Filters[0].IsSelected = true;
            await LoadHistoryAsync(1);
        }
    }

    [RelayCommand]
    private async Task PickFilterAsync(Choice<QuestStatus?> filter)
    {
        foreach (var f in Filters) f.IsSelected = f == filter;
        await LoadHistoryAsync(1);
    }

    [RelayCommand]
    private Task LoadMoreAsync() => LoadHistoryAsync(_page + 1);

    [RelayCommand]
    private Task OpenQuestAsync(Guid id) => navigator.GoToAsync(Routes.Quest, new Dictionary<string, object> { ["id"] = id });

    [RelayCommand]
    private Task BrowseAsync() => navigator.GoToAsync(Routes.Today);

    private async Task LoadHistoryAsync(int page)
    {
        if (LoadingHistory) return;
        LoadingHistory = true;
        try
        {
            var status = Filters.FirstOrDefault(f => f.IsSelected)?.Value;
            var result = await api.HistoryAsync(page, PageSize, status);
            if (page == 1) History.Clear();
            foreach (var q in result.Items) History.Add(new HistoryItem(q));
            _page = result.PageNumber;
            HasMore = result.HasNextPage;
            _historyLoaded = true;
        }
        catch (ApiException)
        {
            // Geçmiş ikincil: hata olursa liste olduğu gibi kalır.
        }
        finally
        {
            LoadingHistory = false;
            OnPropertyChanged(nameof(NoHistory));
        }
    }

    protected internal override void OnLanguageChanged()
    {
        Choices.Refresh(Filters);
        if (IsLoaded) _ = RefreshAsync();
    }
}

/// <summary>Geçmiş satırı: kategori, başlık, durum ve tarih.</summary>
public sealed class HistoryItem(Quest quest)
{
    public Guid Id => quest.Id;
    public string Title => quest.Title;
    public string Icon => Labels.Category(quest.Category).Icon;
    public string ColorKey => Labels.Category(quest.Category).ColorKey;
    public string Category => Labels.Category(quest.Category).Label;
    public string Status => Labels.Status(quest.Status);
    public string StatusKey => quest.Status.ToString();
    public string Date => Format.Date(quest.CompletedAt ?? quest.OfferedAt, "d MMM");
}

public sealed record CategoryProgressItem(string Label, string Icon, string ColorKey, string Line, double Ratio);

public sealed record AchievementItem(string Title, string Description, bool Unlocked)
{
    public string Icon => Unlocked ? "trophy" : "lock";
}

public sealed record XpFeedItem(string Xp, string Text, string Date);

/// <summary>İlerleme (web: features/progress): Life XP, kategori seviyeleri, başarımlar ve son kazanımlar.</summary>
public sealed partial class ProgressViewModel(LifeQuestApi api) : ViewModelBase
{
    public ObservableCollection<CategoryProgressItem> Categories { get; } = [];
    public ObservableCollection<AchievementItem> Achievements { get; } = [];
    public ObservableCollection<XpFeedItem> Recent { get; } = [];

    [ObservableProperty]
    public partial Progress? Progress { get; set; }

    public int Level => Progress?.LifeLevel ?? 1;
    public double LevelProgress => Progress?.LevelProgress ?? 0;
    public string LifeXp => Progress is { } p ? Format.Number(p.LifeXp) : "0";
    public string ToNext => Progress is { } p ? S.Progress.ToNext(Math.Max(0, p.NextLevelXp - p.LifeXp)) : "";
    public string Experiences => Progress is { } p ? S.Progress.Experiences(p.TotalCompleted) : "";
    public string AchievementCount => $"{Achievements.Count(a => a.Unlocked)} / {Achievements.Count}";
    public bool HasRecent => Recent.Count > 0;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(async () =>
    {
        var progress = api.ProgressAsync();
        var achievements = api.AchievementsAsync();
        Progress = await progress;

        Categories.Clear();
        foreach (var c in Progress.Categories)
        {
            var meta = Labels.Category(c.Category);
            Categories.Add(new(meta.Label, meta.Icon, meta.ColorKey, S.Progress.LevelLine(c.Level, c.Xp, c.NextLevelXp),
                c.NextLevelXp > 0 ? Math.Clamp(c.Xp / (double)c.NextLevelXp, 0, 1) : 0));
        }

        Achievements.Clear();
        foreach (var a in await achievements) Achievements.Add(new(a.Title, a.Description, a.Unlocked));

        Recent.Clear();
        foreach (var e in Progress.RecentXp) Recent.Add(new($"+{e.LifeXp}", e.Description, Format.Date(e.At, "d MMM")));

        OnPropertyChanged(string.Empty);
    });

    protected internal override void OnLanguageChanged()
    {
        if (IsLoaded) _ = RefreshAsync();
    }
}
