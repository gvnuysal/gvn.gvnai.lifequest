using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

/// <summary>Beş adımlı onboarding (web: features/onboarding): hedefler, ilgi alanları, kartlar, zaman/bütçe, keşif.</summary>
public sealed partial class OnboardingViewModel(
    LifeQuestApi api, ProfileState profile, AppFlow flow, IToast toast, IAppInfo appInfo) : ViewModelBase
{
    public const int StepCount = 5;

    public ObservableCollection<Choice<LifeCategory>> Goals { get; } = Choices.Categories();
    public ObservableCollection<InterestGroup> InterestGroups { get; } = [];
    public ObservableCollection<Choice<int>> WeeklyTimes { get; } = Choices.WeeklyTimes();
    public ObservableCollection<Choice<CostBand>> Budgets { get; } = Choices.Costs();
    public ObservableCollection<Choice<PhysicalEffort>> Efforts { get; } = Choices.EffortLimits();
    public ObservableCollection<Choice<DiscoveryRadius>> Radii { get; } = Choices.Radii();

    private IReadOnlyList<StarterCard> _cards = [];
    private readonly Dictionary<string, StarterReactionType> _reactions = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle), nameof(StepLabel), nameof(IsLastStep), nameof(CanGoBack), nameof(NextLabel), nameof(CanContinue), nameof(Progress))]
    public partial int Step { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCard), nameof(CardDuration), nameof(CardCost), nameof(CardCategory), nameof(CardsDone), nameof(LikedText))]
    public partial StarterCard? CurrentCard { get; set; }

    [ObservableProperty]
    public partial string City { get; set; } = "";

    public string Greeting => S.Onboarding.GoalsTitle(FirstName);
    public string StepTitle => S.Onboarding.Steps[Step];
    public string StepLabel => S.Onboarding.StepOf(Step + 1, StepCount);
    public double Progress => (Step + 1) / (double)StepCount;
    public bool IsLastStep => Step == StepCount - 1;
    public bool CanGoBack => Step > 0;
    public string NextLabel => IsLastStep ? S.Onboarding.Start : S.Onboarding.Next;

    public bool CanContinue => Step switch
    {
        0 => Goals.Any(g => g.IsSelected),
        1 => InterestGroups.SelectMany(g => g).Any(i => i.IsSelected),
        _ => true
    };

    public bool HasCard => CurrentCard is not null;
    public bool CardsDone => CurrentCard is null && _cards.Count > 0;
    public string CardDuration => CurrentCard is { } c ? Format.Duration(c.MinMinutes, c.MaxMinutes) : "";
    public string CardCost => CurrentCard is { } c ? Labels.Cost(c.Cost).Short : "";
    public CategoryMeta? CardCategory => CurrentCard is { } c ? Labels.Category(c.Category) : null;
    public string LikedText => S.Onboarding.LikedCards(_reactions.Values.Count(r => r == StarterReactionType.Like));

    private string FirstName => profile.Current?.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoaded) return;
        Choices.Select(WeeklyTimes, 300);
        Choices.Select(Budgets, CostBand.Low);
        Choices.Select(Efforts, PhysicalEffort.Vigorous);
        Choices.Select(Radii, DiscoveryRadius.Explore);

        await LoadAsync(async () =>
        {
            var interests = api.InterestsAsync();
            var cards = api.StarterCardsAsync();
            await profile.LoadAsync();
            InterestGroups.Clear();
            foreach (var group in InterestGroup.Build(await interests))
            {
                foreach (var item in group) item.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanContinue));
                InterestGroups.Add(group);
            }

            // Kartlar isteğe bağlı: yüklenemezse adım boş geçilir.
            try { _cards = await cards; } catch (ApiException) { _cards = []; }
            CurrentCard = _cards.FirstOrDefault();
            OnPropertyChanged(nameof(Greeting));
        });
    }

    [RelayCommand]
    private void ToggleGoal(Choice<LifeCategory> goal)
    {
        goal.IsSelected = !goal.IsSelected;
        OnPropertyChanged(nameof(CanContinue));
    }

    [RelayCommand]
    private static void CycleInterest(InterestChoice interest) => interest.Cycle();

    [RelayCommand]
    private void Like() => React(StarterReactionType.Like);

    [RelayCommand]
    private void Dislike() => React(StarterReactionType.Dislike);

    [RelayCommand]
    private void SkipCard() => React(null);

    [RelayCommand]
    private void RestartCards()
    {
        _reactions.Clear();
        CurrentCard = _cards.FirstOrDefault();
    }

    [RelayCommand]
    private void PickWeeklyTime(Choice<int> choice) => Choices.Select(WeeklyTimes, choice.Value);

    [RelayCommand]
    private void PickBudget(Choice<CostBand> choice) => Choices.Select(Budgets, choice.Value);

    [RelayCommand]
    private void PickEffort(Choice<PhysicalEffort> choice) => Choices.Select(Efforts, choice.Value);

    [RelayCommand]
    private void PickRadius(Choice<DiscoveryRadius> choice) => Choices.Select(Radii, choice.Value);

    [RelayCommand]
    private void Back() => Step = Math.Max(0, Step - 1);

    [RelayCommand]
    private async Task NextAsync()
    {
        if (!CanContinue) return;
        if (!IsLastStep)
        {
            Step++;
            return;
        }

        await RunAsync(async () =>
        {
            var city = City.Trim();
            var saved = await api.CompleteOnboardingAsync(new OnboardingRequest(
                Goals.Where(g => g.IsSelected).Select(g => g.Value).ToList(),
                InterestGroups.SelectMany(g => g).Where(i => i.IsSelected).Select(i => new InterestSelection(i.Code, i.Weight)).ToList(),
                WeeklyTimes.First(w => w.IsSelected).Value,
                Budgets.First(b => b.IsSelected).Value,
                Radii.First(r => r.IsSelected).Value,
                city.Length > 0 ? city : null,
                appInfo.TimeZoneId,
                Efforts.First(e => e.IsSelected).Value,
                _reactions.Select(r => new StarterReaction(r.Key, r.Value)).ToList()));
            profile.Set(saved);
            await flow.EnterAsync(reloadProfile: false);
        }, toast);
    }

    private void React(StarterReactionType? reaction)
    {
        if (CurrentCard is not { } card) return;
        if (reaction is { } r) _reactions[card.Code] = r;
        else _reactions.Remove(card.Code);

        var index = _cards.ToList().IndexOf(card) + 1;
        CurrentCard = index < _cards.Count ? _cards[index] : null;
    }

    protected internal override void OnLanguageChanged()
    {
        Choices.Refresh(Goals);
        Choices.Refresh(WeeklyTimes);
        Choices.Refresh(Budgets);
        Choices.Refresh(Efforts);
        Choices.Refresh(Radii);
        OnPropertyChanged(string.Empty);
    }
}
