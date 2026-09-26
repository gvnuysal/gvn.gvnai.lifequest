using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Localization;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

public sealed record QuestFact(string Icon, string Label, string Value);

/// <summary>Skor çubuğu: 0–1 oran; cezalar kırmızı ve eksi işaretli.</summary>
public sealed record ScoreBar(string Label, double Ratio, string Value, bool Penalty);

public sealed record PlaceItem(string Name, string? When, string? Address, string? Note, string? Url)
{
    public bool HasWhen => When is not null;
    public bool HasAddress => Address is not null;
    public bool HasNote => Note is not null;
    public bool HasUrl => Url is not null;
}

public sealed record PartyMemberItem(string Name, string State, bool Dropped, bool Completed);

/// <summary>Görev detayı (web: features/quest). Kutlama aynı sayfada katman olarak açılır.</summary>
public sealed partial class QuestViewModel(
    LifeQuestApi api, INavigator navigator, IToast toast, IShare share, IAppInfo appInfo, ReminderService reminders, TimeProvider clock)
    : ViewModelBase
{
    public Guid Id { get; set; }

    public ObservableCollection<QuestFact> Facts { get; } = [];
    public ObservableCollection<ScoreBar> ScoreBars { get; } = [];
    public ObservableCollection<PlaceItem> Places { get; } = [];
    public ObservableCollection<PartyMemberItem> PartyMembers { get; } = [];
    public ObservableCollection<Choice<SkipReason>> SkipReasons { get; } =
        [.. Labels.SkipReasons.Select(r => new Choice<SkipReason>(r, () => Labels.SkipReason(r).Label, () => NullIfEmpty(Labels.SkipReason(r).Hint)))];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Quest), nameof(HasQuest))]
    public partial QuestDetail? Detail { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    [ObservableProperty]
    public partial bool WhyOpen { get; set; }

    [ObservableProperty]
    public partial bool SkipOpen { get; set; }

    [ObservableProperty]
    public partial bool PlanOpen { get; set; }

    [ObservableProperty]
    public partial DateTime PlanDate { get; set; }

    [ObservableProperty]
    public partial TimeSpan PlanTime { get; set; }

    /// <summary>Tamamlandı sonrası puan (kutlama kapatıldıysa sonradan).</summary>
    [ObservableProperty]
    public partial int Rating { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCelebrating))]
    public partial CelebrationViewModel? Celebration { get; set; }

    public Quest? Quest => Detail?.Quest;
    public bool HasQuest => Quest is not null;
    public bool IsCelebrating => Celebration is not null;

    // Başlık ve rozetler
    public string Title => Quest?.Title ?? "";
    public string Description => Quest?.Description ?? "";
    public string Explanation => Quest?.Explanation ?? "";
    public string Icon => Quest is { } q ? Labels.Category(q.Category).Icon : "compass";
    public string ColorKey => Quest is { } q ? Labels.Category(q.Category).ColorKey : "Explorer";
    public string CategoryLabel => Quest is { } q ? Labels.Category(q.Category).Label : "";
    public bool HasSecondary => Quest?.SecondaryCategory is not null;
    public string SecondaryLabel => Quest?.SecondaryCategory is { } c ? Labels.Category(c).Label : "";
    public string SecondaryColorKey => Quest?.SecondaryCategory is { } c ? Labels.Category(c).ColorKey : "Explorer";
    public bool IsExploration => Quest?.IsExploration == true;

    // Ödül
    public string LifeXp => Quest is { } q ? $"+{q.Reward.LifeXp}" : "";
    public string PrimaryXp => Quest is { } q ? $"+{q.Reward.PrimaryCategoryXp} {CategoryLabel}" : "";
    public bool HasSecondaryXp => Quest is { SecondaryCategory: not null, Reward.SecondaryCategoryXp: > 0 };
    public string SecondaryXp => Quest is { } q ? $"+{q.Reward.SecondaryCategoryXp} {SecondaryLabel}" : "";

    // Durum
    public bool IsOffered => Quest?.Status == QuestStatus.Offered;
    public bool IsAccepted => Quest?.Status == QuestStatus.Accepted;
    public bool IsCompleted => Quest?.Status == QuestStatus.Completed;
    public bool IsOpen => IsOffered || IsAccepted;
    public bool IsClosedOther => Quest is { Status: QuestStatus.Skipped or QuestStatus.Expired };
    public string StatusText => Quest is not { } q ? "" : q.Status switch
    {
        QuestStatus.Offered => S.Quest.OfferedHint,
        QuestStatus.Accepted => S.Quest.InProgress(Format.Remaining(q.ExpiresAt)),
        QuestStatus.Completed => S.Quest.CompletedOn(q.CompletedAt is { } at ? Format.Date(at) : ""),
        _ => Labels.Status(q.Status)
    };
    public string StatusIcon => Quest?.Status switch
    {
        QuestStatus.Accepted => "hourglass",
        QuestStatus.Completed => "check",
        _ => "info"
    };
    public string StatusKey => Quest?.Status.ToString() ?? "";
    public bool HasRating => Quest?.Rating is not null;
    public string RatingText => Quest?.Rating is { } r ? S.Quest.YourRating(r) : "";
    public bool CanRateLater => IsCompleted && !HasRating && !IsCelebrating;

    // Aksiyonlar
    public string SkipLabel => IsOffered ? S.Quest.Skip : S.Quest.Drop;
    public bool IsPlanned => Quest?.PlannedAt is not null;
    public string PlanLabel => IsPlanned ? S.Quest.ChangePlan : S.Quest.Plan;
    public string PlannedText => Quest?.PlannedAt is { } at ? S.Quest.PlannedFor(Format.Date(at, "dddd d MMMM HH:mm")) : "";
    public DateTime PlanMin => clock.GetLocalNow().Date;
    public DateTime PlanMax => Quest is { } q ? q.ExpiresAt.ToLocalTime().AddMinutes(-1) : PlanMin.AddDays(7);
    public bool CanConfirmSkip => SkipReasons.Any(r => r.IsSelected);

    // Parti
    public bool ShowParty => IsAccepted || Detail?.Party is not null;
    public bool HasParty => Detail?.Party is not null;
    public bool PartyCompleted => Detail?.Party?.Status == PartyStatus.Completed;
    public bool PartyJoinable => Detail?.Party?.IsJoinable == true;
    public bool CanCreateParty => !HasParty && IsAccepted;
    public string PartyHint => Detail?.Party is { } p ? S.Party.ShareHint(p.Members.Count, p.MaxMembers) : "";
    public string InviteLink => Detail?.Party is { } p ? $"{appInfo.WebBaseUrl.TrimEnd('/')}/party/{p.InviteCode}" : "";

    public bool HasPlaces => Places.Count > 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        NotFound = false;
        try
        {
            IsBusy = true;
            SetDetail(await api.QuestAsync(Id));
            IsLoaded = true;
        }
        catch (ApiException)
        {
            NotFound = Detail is null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleWhy() => WhyOpen = !WhyOpen;

    [RelayCommand]
    private Task AcceptAsync() => ActAsync(async () =>
    {
        Patch(await api.AcceptAsync(Id));
        toast.Success(S.Quest.ToastAccepted);
    });

    [RelayCommand]
    private Task CompleteAsync() => ActAsync(async () =>
    {
        var result = await api.CompleteAsync(Id);
        Patch(result.Quest);
        Celebration = new CelebrationViewModel(result, SubmitCelebrationAsync);
        OnPropertyChanged(nameof(CanRateLater));
        await reminders.RescheduleAsync(completedToday: true);
    });

    [RelayCommand]
    private void OpenSkip()
    {
        Choices.Select(SkipReasons, (SkipReason)(-1));
        OnPropertyChanged(nameof(CanConfirmSkip));
        SkipOpen = true;
    }

    [RelayCommand]
    private void PickSkipReason(Choice<SkipReason> reason)
    {
        Choices.Select(SkipReasons, reason.Value);
        OnPropertyChanged(nameof(CanConfirmSkip));
    }

    [RelayCommand]
    private Task ConfirmSkipAsync() => ActAsync(async () =>
    {
        if (SkipReasons.FirstOrDefault(r => r.IsSelected) is not { } reason) return;
        Patch(await api.SkipAsync(Id, reason.Value));
        SkipOpen = false;
        toast.Show(S.Quest.ToastSkipped);
    });

    [RelayCommand]
    private Task SaveForLaterAsync() => ActAsync(async () =>
    {
        await api.SaveAsync(Id);
        toast.Success(S.Quest.ToastSaved);
    });

    [RelayCommand]
    private void OpenPlan()
    {
        var now = clock.GetLocalNow().DateTime;
        var start = Quest?.PlannedAt?.ToLocalTime() ?? now.Date.AddDays(1).AddHours(10);
        if (start > PlanMax) start = PlanMax;
        PlanDate = start.Date;
        PlanTime = start.TimeOfDay;
        PlanOpen = true;
    }

    [RelayCommand]
    private Task SavePlanAsync() => ActAsync(async () =>
    {
        Patch(await api.PlanAsync(Id, PlanDate.Date + PlanTime));
        PlanOpen = false;
        toast.Success(S.Quest.ToastPlanned);
    });

    [RelayCommand]
    private Task ClearPlanAsync() => ActAsync(async () =>
    {
        Patch(await api.PlanAsync(Id, null));
        PlanOpen = false;
        toast.Show(S.Quest.ToastPlanRemoved);
    });

    [RelayCommand]
    private Task AddToCalendarAsync() => ActAsync(async () =>
        await share.ShareFileAsync(MobileStrings.Instance.CalendarTitle, await api.CalendarAsync(Id)));

    [RelayCommand]
    private Task RateLaterAsync() => Rating is < 1 or > 5 ? Task.CompletedTask : SendFeedbackAsync(Rating, null);

    [RelayCommand]
    private Task CreatePartyAsync() => ActAsync(async () =>
    {
        var party = await api.CreatePartyAsync(Id);
        SetDetail(Detail! with { Party = party });
    });

    [RelayCommand]
    private async Task ShareInviteAsync()
    {
        if (Detail?.Party is not { } p) return;
        await share.ShareTextAsync(MobileStrings.Instance.ShareInvite, $"{S.Party.ShareText(p.QuestTitle)} {InviteLink}");
    }

    [RelayCommand]
    private async Task OpenPlaceAsync(PlaceItem place)
    {
        if (place.Url is { } url) await share.OpenBrowserAsync(url);
    }

    [RelayCommand]
    private Task BackAsync() => navigator.BackAsync();

    [RelayCommand]
    private Task GoTodayAsync() => navigator.GoToAsync(Routes.Today);

    private async Task SubmitCelebrationAsync(int? rating, FeedbackPreference? preference)
    {
        if (rating is null && preference is null)
        {
            Celebration = null;
            OnPropertyChanged(nameof(CanRateLater));
            return;
        }

        await SendFeedbackAsync(rating, preference, () =>
        {
            Celebration = null;
            OnPropertyChanged(nameof(CanRateLater));
        });
    }

    private Task SendFeedbackAsync(int? rating, FeedbackPreference? preference, Action? after = null) => ActAsync(async () =>
    {
        var result = await api.FeedbackAsync(Id, rating, preference);
        Patch(result.Quest);
        after?.Invoke();
        toast.Success(result.NewAchievements.FirstOrDefault() is { } a ? S.Quest.ToastAchievement(a.Title) : S.Quest.ToastThanks);
    });

    /// <summary>Aksiyon; iş kuralı hatasında (ör. 409) mesaj gösterilir ve güncel durum yeniden yüklenir.</summary>
    private async Task ActAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (ApiException ex)
        {
            toast.Error(ex.Message);
            IsBusy = false;
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Patch(Quest quest)
    {
        if (Detail is { } d) SetDetail(d with { Quest = quest });
    }

    private void SetDetail(QuestDetail detail)
    {
        Detail = detail;
        var q = detail.Quest;

        Facts.Clear();
        var f = S.Quest.Facts;
        Facts.Add(new("clock", f.Duration, Format.Duration(q.MinMinutes, q.MaxMinutes)));
        Facts.Add(new("coins", f.Cost, Labels.Cost(q.Cost).Label));
        Facts.Add(new("target", f.Difficulty, Labels.Difficulty(q.Difficulty)));
        Facts.Add(new("flag", f.Type, Labels.QuestType(q.Type)));
        Facts.Add(new("activity", f.Effort, Labels.Effort(q.Effort).Label));

        ScoreBars.Clear();
        foreach (var (_, label, penalty, value) in Labels.ScoreComponents)
        {
            var v = value(detail.Score);
            ScoreBars.Add(new(label, Math.Clamp(v, 0, 1), $"{(penalty && v > 0 ? "−" : "")}{Math.Round(v * 100):0}", penalty));
        }

        Places.Clear();
        foreach (var p in detail.NearbyPlaces)
            Places.Add(new(p.Name, p is { Kind: LocalPlaceKind.Event, StartsAt: { } at } ? Format.Date(at, "dddd d MMMM HH:mm") : null,
                p.Address, p.Note, p.Url));

        PartyMembers.Clear();
        if (detail.Party is { } party)
        {
            var s = S.Party;
            foreach (var m in party.Members)
            {
                var name = m.DisplayName + (m.IsYou ? $" {s.You}" : "") + (m.IsHost ? $" {s.Host}" : "");
                var state = m.Completed ? s.Done + (m.BonusXp > 0 ? $" · +{m.BonusXp} XP" : "") : m.Dropped ? s.Dropped : s.InProgress;
                PartyMembers.Add(new(name, state, m.Dropped, m.Completed));
            }
        }

        OnPropertyChanged(string.Empty);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    protected internal override void OnLanguageChanged()
    {
        Choices.Refresh(SkipReasons);
        if (IsLoaded) _ = LoadAsync();
    }
}

/// <summary>Tamamlama kutlaması (web: quest/celebration): XP sayacı, seviye, başarımlar, puan ve tercih.</summary>
public sealed partial class CelebrationViewModel(QuestCompletion completion, Func<int?, FeedbackPreference?, Task> submit)
    : ViewModelBase
{
    public QuestCompletion Completion { get; } = completion;

    public string Heading => Completion.AlreadyCompleted ? S.Celebration.AlreadyDone : S.Celebration.Great;
    public string QuestTitle => Completion.Quest.Title;
    public int TargetXp => Completion.Quest.Reward.LifeXp;
    public bool HasPartyBonus => Completion.PartyBonusXp > 0;
    public string PartyBonus => S.Celebration.PartyBonus(Completion.PartyBonusXp);
    public bool LeveledUp => Completion.LeveledUp;
    public string LevelUp => S.Celebration.LevelUp(Completion.LifeLevel);
    public IReadOnlyList<Achievement> Achievements => Completion.NewAchievements;
    public bool HasAchievements => Achievements.Count > 0;

    /// <summary>Animasyonla artan XP (View zamanlayıcıyla ilerletir).</summary>
    [ObservableProperty]
    public partial int ShownXp { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubmitLabel))]
    public partial int Rating { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WantsMore), nameof(WantsLess), nameof(SubmitLabel))]
    public partial FeedbackPreference? Preference { get; set; }

    public bool WantsMore => Preference == FeedbackPreference.MoreLikeThis;
    public bool WantsLess => Preference == FeedbackPreference.LessLikeThis;
    public string SubmitLabel => Rating > 0 || Preference is not null ? S.Celebration.SaveContinue : S.Celebration.Continue;

    /// <summary>0–1 ilerlemeye göre XP (ease-out-cubic, web ile aynı eğri).</summary>
    public void Animate(double t) => ShownXp = (int)Math.Round(TargetXp * (1 - Math.Pow(1 - Math.Clamp(t, 0, 1), 3)));

    [RelayCommand]
    private void ToggleMore() => Preference = WantsMore ? null : FeedbackPreference.MoreLikeThis;

    [RelayCommand]
    private void ToggleLess() => Preference = WantsLess ? null : FeedbackPreference.LessLikeThis;

    [RelayCommand]
    private Task SubmitAsync() => submit(Rating > 0 ? Rating : null, Preference);
}
