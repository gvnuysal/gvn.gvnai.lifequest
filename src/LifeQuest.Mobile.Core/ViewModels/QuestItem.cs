using CommunityToolkit.Mvvm.ComponentModel;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.ViewModels;

/// <summary>Görev kartının gösterim modeli (web: ui/quest-card). Metinler o anki dilde hesaplanır.</summary>
public sealed class QuestItem(Quest quest) : ObservableObject
{
    public Quest Quest { get; } = quest;

    public Guid Id => Quest.Id;
    public string Title => Quest.Title;
    public string Explanation => Quest.Explanation;
    public string Icon => Labels.Category(Quest.Category).Icon;
    public string ColorKey => Labels.Category(Quest.Category).ColorKey;
    public string Kicker => $"{Labels.Category(Quest.Category).Label} · {Labels.QuestType(Quest.Type)}";
    public bool IsExploration => Quest.IsExploration;
    public string Xp => $"+{Quest.Reward.LifeXp}";
    public string Duration => Format.Duration(Quest.MinMinutes, Quest.MaxMinutes);
    public string Cost => Labels.Cost(Quest.Cost).Short;

    public bool IsPlanned => Quest is { Status: QuestStatus.Accepted, PlannedAt: not null };
    public bool ShowRemaining => Quest is { Status: QuestStatus.Accepted, PlannedAt: null };
    public bool ShowStatus => Quest.Status is not (QuestStatus.Offered or QuestStatus.Accepted);

    public string Planned => Quest.PlannedAt is { } at ? Format.Date(at, "ddd d MMM HH:mm") : "";
    public string Remaining => Format.Remaining(Quest.ExpiresAt);
    public string Status => Labels.Status(Quest.Status);
    public string StatusKey => Quest.Status.ToString();

    public string AccessibleName => $"{Title}, {Labels.Category(Quest.Category).Label}";

    public void Refresh() => OnPropertyChanged(string.Empty);
}
