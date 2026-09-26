using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.ViewModels;

/// <summary>Seçilebilir kart/çip (hedef, bütçe, keşif modu…). Metin getter'ları dil değişince yenilenir.</summary>
public sealed partial class Choice<T>(T value, Func<string> label, Func<string?>? hint = null, string? icon = null, string? colorKey = null)
    : ObservableObject
{
    public T Value { get; } = value;
    public string Label => label();
    public string? Hint => hint?.Invoke();
    public string? Icon { get; } = icon;
    public string? ColorKey { get; } = colorKey;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Hint));
    }
}

public static class Choices
{
    public static ObservableCollection<Choice<LifeCategory>> Categories(bool withDescription = true) =>
    [
        .. Labels.CategoryOrder.Select(c =>
        {
            var meta = Labels.Category(c);
            return new Choice<LifeCategory>(c, () => Labels.Category(c).Label,
                withDescription ? () => Labels.Category(c).Description : null, meta.Icon, meta.ColorKey);
        })
    ];

    public static ObservableCollection<Choice<CostBand>> Costs() =>
        [.. Labels.CostOrder.Select(c => new Choice<CostBand>(c, () => Labels.Cost(c).Label, () => Labels.Cost(c).Hint, "coins"))];

    public static ObservableCollection<Choice<DiscoveryRadius>> Radii() =>
        [.. Labels.RadiusOrder.Select(r => new Choice<DiscoveryRadius>(r, () => Labels.Radius(r).Label, () => Labels.Radius(r).Description, Labels.RadiusIcon(r)))];

    public static ObservableCollection<Choice<int>> WeeklyTimes() =>
        [.. Labels.WeeklyTimeOptions.Select(m => new Choice<int>(m, () => Labels.WeeklyTime(m).Label, () => Labels.WeeklyTime(m).Hint, "clock"))];

    public static ObservableCollection<Choice<PhysicalEffort>> EffortLimits() =>
        [.. Labels.EffortLimits.Select(e => new Choice<PhysicalEffort>(e, () => Labels.EffortLimit(e).Label, () => Labels.EffortLimit(e).Hint, "activity"))];

    /// <summary>Tek seçim: verilen değeri seçili yapar.</summary>
    public static void Select<T>(IEnumerable<Choice<T>> choices, T value)
    {
        foreach (var c in choices) c.IsSelected = EqualityComparer<T>.Default.Equals(c.Value, value);
    }

    public static void Refresh<T>(IEnumerable<Choice<T>> choices)
    {
        foreach (var c in choices) c.Refresh();
    }
}

/// <summary>
/// İlgi alanı seçimi (web interest-picker): dokundukça yok → ilgileniyorum (0.6) → çok seviyorum (0.9) → yok.
/// </summary>
public sealed partial class InterestChoice(Interest interest) : ObservableObject
{
    public const double LikeWeight = 0.6;
    public const double LoveWeight = 0.9;

    public Interest Interest { get; } = interest;
    public string Code => Interest.Code;
    public string Name => Interest.Name;
    public string ColorKey => Labels.Category(Interest.Category).ColorKey;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelected), nameof(IsLoved), nameof(StateLabel))]
    public partial double Weight { get; set; }

    public bool IsSelected => Weight > 0;
    public bool IsLoved => Weight >= LoveWeight;

    public string StateLabel => IsLoved ? Localizer.Instance.S.Ui.InterestLove
        : IsSelected ? Localizer.Instance.S.Ui.InterestLike
        : Localizer.Instance.S.Ui.InterestNone;

    public void Cycle() => Weight = Weight == 0 ? LikeWeight : Weight < LoveWeight ? LoveWeight : 0;
}

public sealed class InterestGroup(LifeCategory category, IEnumerable<InterestChoice> items) : ObservableCollection<InterestChoice>(items)
{
    public LifeCategory Category { get; } = category;
    public string Label => Labels.Category(Category).Label;
    public string Icon => Labels.Category(Category).Icon;
    public string ColorKey => Labels.Category(Category).ColorKey;

    public static List<InterestGroup> Build(IEnumerable<Interest> interests, IReadOnlyDictionary<string, double>? weights = null)
    {
        var list = interests.ToList();
        return Labels.CategoryOrder
            .Select(c => new InterestGroup(c, list.Where(i => i.Category == c).Select(i => new InterestChoice(i)
            {
                Weight = weights?.GetValueOrDefault(i.Code) ?? 0
            })))
            .Where(g => g.Count > 0)
            .ToList();
    }
}
