using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

public sealed class SavedItem(SavedQuest saved)
{
    public SavedQuest Saved { get; } = saved;
    public string Title => Saved.Title;
    public string Description => Saved.Description;
    public string Icon => Labels.Category(Saved.Category).Icon;
    public string ColorKey => Labels.Category(Saved.Category).ColorKey;
    public string Kicker => $"{Labels.Category(Saved.Category).Label} · {Labels.QuestType(Saved.Type)}";
    public string Duration => Format.Duration(Saved.MinMinutes, Saved.MaxMinutes);
    public string Cost => Labels.Cost(Saved.Cost).Label;
    public bool IsAvailable => Saved.IsAvailable;
    public bool IsUnavailable => !Saved.IsAvailable;
}

/// <summary>"Sonra yaparım" listesi (web: features/saved).</summary>
public sealed partial class SavedViewModel(LifeQuestApi api, INavigator navigator, IToast toast) : ViewModelBase
{
    public ObservableCollection<SavedItem> Items { get; } = [];

    public bool IsEmpty => IsLoaded && Items.Count == 0;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(async () =>
        {
            var list = await api.SavedAsync();
            Items.Clear();
            foreach (var s in list) Items.Add(new SavedItem(s));
        });
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private Task StartAsync(SavedItem item) => RunAsync(async () =>
    {
        var quest = await api.StartSavedAsync(item.Saved.TemplateId);
        Items.Remove(item);
        toast.Success(S.Saved.Started);
        await navigator.GoToAsync(Routes.Quest, new Dictionary<string, object> { ["id"] = quest.Id });
    }, toast);

    [RelayCommand]
    private Task RemoveAsync(SavedItem item) => RunAsync(async () =>
    {
        await api.RemoveSavedAsync(item.Saved.TemplateId);
        Items.Remove(item);
        OnPropertyChanged(nameof(IsEmpty));
    }, toast);

    [RelayCommand]
    private Task BrowseAsync() => navigator.GoToAsync(Routes.Today);

    protected internal override void OnLanguageChanged()
    {
        if (IsLoaded) _ = RefreshAsync();
    }
}

public sealed class IdeaItem(MyIdea idea)
{
    public MyIdea Idea { get; } = idea;
    public string Title => Idea.Title;
    public string Meta => $"{Labels.Category(Idea.Category).Label} · {Idea.Minutes} {Localization.Localizer.Instance.S.Format.Min} · {Labels.Cost(Idea.Cost).Label} · {Format.Date(Idea.SubmittedAt, "d MMM")}";
    public string Status => Localization.Localizer.Instance.S.Ideas.Status[Idea.Status.ToString()];
    public string StatusKey => Idea.Status.ToString();
    public string? ReviewNote => Idea.ReviewNote;
    public bool HasReviewNote => !string.IsNullOrWhiteSpace(Idea.ReviewNote);
    public bool CanWithdraw => Idea.Status == IdeaStatus.Pending;
}

/// <summary>Topluluk fikri gönderme ve kendi fikirleri (web: features/ideas).</summary>
public sealed partial class IdeasViewModel(LifeQuestApi api, IToast toast) : ViewModelBase
{
    public ObservableCollection<Choice<LifeCategory>> Categories { get; } = Choices.Categories(withDescription: false);
    public ObservableCollection<Choice<CostBand>> Costs { get; } = Choices.Costs();
    public ObservableCollection<IdeaItem> Ideas { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string IdeaTitle { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit), nameof(DescriptionCount))]
    public partial string Description { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string Minutes { get; set; } = "45";

    [ObservableProperty]
    public partial bool IsOutdoor { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    public string DescriptionCount => $"{Description.Trim().Length} / 300 · {S.Ideas.MinChars}";

    public bool CanSubmit => IdeaTitle.Trim().Length is >= 5 and <= 80
                             && Description.Trim().Length is >= 30 and <= 300
                             && int.TryParse(Minutes, out var m) && m is >= 5 and <= 600;

    public bool NoIdeas => IsLoaded && Ideas.Count == 0;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!Categories.Any(c => c.IsSelected)) ResetForm();
        await LoadAsync(async () =>
        {
            var list = await api.MyIdeasAsync();
            Ideas.Clear();
            foreach (var i in list) Ideas.Add(new IdeaItem(i));
        });
        OnPropertyChanged(nameof(NoIdeas));
    }

    [RelayCommand]
    private void PickCategory(Choice<LifeCategory> choice) => Choices.Select(Categories, choice.Value);

    [RelayCommand]
    private void PickCost(Choice<CostBand> choice) => Choices.Select(Costs, choice.Value);

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit || IsBusy) return;
        Error = null;
        IsBusy = true;
        try
        {
            var idea = await api.SubmitIdeaAsync(new IdeaRequest(IdeaTitle.Trim(), Description.Trim(),
                Categories.First(c => c.IsSelected).Value, int.Parse(Minutes), Costs.First(c => c.IsSelected).Value, IsOutdoor));
            Ideas.Insert(0, new IdeaItem(idea));
            ResetForm();
            toast.Success(S.Ideas.Sent);
            OnPropertyChanged(nameof(NoIdeas));
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
    private Task WithdrawAsync(IdeaItem item) => RunAsync(async () =>
    {
        await api.WithdrawIdeaAsync(item.Idea.Id);
        Ideas.Remove(item);
        OnPropertyChanged(nameof(NoIdeas));
    }, toast);

    private void ResetForm()
    {
        IdeaTitle = "";
        Description = "";
        Minutes = "45";
        IsOutdoor = false;
        Choices.Select(Categories, LifeCategory.Explorer);
        Choices.Select(Costs, CostBand.Free);
    }

    protected internal override void OnLanguageChanged()
    {
        Choices.Refresh(Categories);
        Choices.Refresh(Costs);
        if (IsLoaded) _ = RefreshAsync();
        OnPropertyChanged(nameof(DescriptionCount));
    }
}

/// <summary>Parti daveti (web: features/party): önizleme ve katılma.</summary>
public sealed partial class PartyViewModel(LifeQuestApi api, INavigator navigator, IToast toast) : ViewModelBase
{
    public string Code { get; set; } = "";

    [ObservableProperty]
    public partial PartyInvite? Invite { get; set; }

    public string Heading => Invite is { } i ? S.Party.Invites(i.HostName) : "";
    public string QuestTitle => Invite?.QuestTitle ?? "";
    public string Icon => Invite is { } i ? Labels.Category(i.Category).Icon : "users";
    public string ColorKey => Invite is { } i ? Labels.Category(i.Category).ColorKey : "Social";
    public string CategoryLabel => Invite is { } i ? Labels.Category(i.Category).Label : "";
    public string Summary => Invite is { } i ? S.Party.Summary(i.MemberCount, i.MaxMembers, Format.Date(i.ExpiresAt, "d MMMM HH:mm")) : "";
    public bool IsMember => Invite is { IsMember: true, MyQuestId: not null };
    public bool CanJoin => Invite is { IsMember: false, IsJoinable: true };
    public bool IsClosed => Invite is not null && !IsMember && !CanJoin;

    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadAsync(async () => Invite = await api.PartyInviteAsync(Code));
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private Task JoinAsync() => RunAsync(async () =>
    {
        var invite = await api.JoinPartyAsync(Code);
        toast.Success(S.Party.Joined);
        await GoQuestAsync(invite.MyQuestId);
    }, toast);

    [RelayCommand]
    private Task GoToQuestAsync() => GoQuestAsync(Invite?.MyQuestId);

    [RelayCommand]
    private Task GoTodayAsync() => navigator.GoToAsync(Routes.Today);

    private Task GoQuestAsync(Guid? id)
        => id is { } questId ? navigator.GoToAsync(Routes.Quest, new Dictionary<string, object> { ["id"] = questId }) : Task.CompletedTask;

    protected internal override void OnLanguageChanged()
    {
        if (IsLoaded) _ = LoadAsync();
    }
}
