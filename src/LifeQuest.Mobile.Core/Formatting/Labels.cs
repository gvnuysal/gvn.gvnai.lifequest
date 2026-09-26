using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.Formatting;

/// <summary>Kategori görsel kimliği: ikon adı ve renk anahtarı (Resources/Styles'taki Cat{Key} renkleri).</summary>
public sealed record CategoryMeta(LifeCategory Category, string Icon, string ColorKey)
{
    public string Label => Localizer.Instance.S.Labels.Categories[Category.ToString()];
    public string Description => Localizer.Instance.S.Labels.CategoryDescriptions[Category.ToString()];
}

public sealed record Option<T>(T Value, string Label, string? Hint = null, string? Icon = null);

/// <summary>Enum → etiket köprüsü (web: core/labels/labels.ts). Metinler sözlükten, yapı (ikon, sıra) buradan gelir.</summary>
public static class Labels
{
    private static LabelsStrings L => Localizer.Instance.S.Labels;

    public static readonly LifeCategory[] CategoryOrder =
        [LifeCategory.Explorer, LifeCategory.Culture, LifeCategory.Learning, LifeCategory.Social, LifeCategory.Fitness, LifeCategory.Creativity];

    public static CategoryMeta Category(LifeCategory category) => category switch
    {
        LifeCategory.Explorer => new(category, "compass", "Explorer"),
        LifeCategory.Culture => new(category, "landmark", "Culture"),
        LifeCategory.Learning => new(category, "book", "Learning"),
        LifeCategory.Social => new(category, "users", "Social"),
        LifeCategory.Fitness => new(category, "activity", "Fitness"),
        _ => new(category, "palette", "Creativity")
    };

    public static ILabelsCostItem Cost(CostBand cost) => L.Cost[cost.ToString()];

    public static ILabelsRadiusItem Radius(DiscoveryRadius radius) => L.Radius[radius.ToString()];

    public static string RadiusIcon(DiscoveryRadius radius) => radius switch
    {
        DiscoveryRadius.Chill => "leaf",
        DiscoveryRadius.Explore => "compass",
        _ => "sparkles"
    };

    public static string QuestType(QuestType type) => L.QuestTypes[type.ToString()];
    public static string Difficulty(Difficulty difficulty) => L.Difficulties[difficulty.ToString()];
    public static string Status(QuestStatus status) => L.Statuses[status.ToString()];
    public static ILabelsSkipReasonsItem SkipReason(SkipReason reason) => L.SkipReasons[reason.ToString()];
    public static ILabelsEffortItem Effort(PhysicalEffort effort) => L.Effort[effort.ToString()];
    public static string Notification(NotificationPreference preference) => L.Notifications[preference.ToString()];

    public static readonly CostBand[] CostOrder = [CostBand.Free, CostBand.Low, CostBand.Medium, CostBand.High];
    public static readonly DiscoveryRadius[] RadiusOrder = [DiscoveryRadius.Chill, DiscoveryRadius.Explore, DiscoveryRadius.SurpriseMe];
    public static readonly SkipReason[] SkipReasons =
        [Api.SkipReason.NotInterested, Api.SkipReason.TooExpensive, Api.SkipReason.NoTime, Api.SkipReason.TooFar, Api.SkipReason.NotToday, Api.SkipReason.Other];

    /// <summary>Haftalık zaman seçenekleri (dakika).</summary>
    public static readonly int[] WeeklyTimeOptions = [120, 300, 600, 900];

    public static ILabelsWeeklyTimeItem WeeklyTime(int minutes) => L.WeeklyTime[minutes.ToString(System.Globalization.CultureInfo.InvariantCulture)];

    /// <summary>Profilde seçilebilen efor üst sınırı.</summary>
    public static readonly PhysicalEffort[] EffortLimits = [PhysicalEffort.Light, PhysicalEffort.Moderate, PhysicalEffort.Vigorous];

    public static ILabelsEffortLimitItem EffortLimit(PhysicalEffort effort) => L.EffortLimit[effort.ToString()];

    /// <summary>Skor bileşenleri: anahtar, etiket, ceza mı.</summary>
    public static IReadOnlyList<(string Key, string Label, bool Penalty, Func<ScoreBreakdown, double> Value)> ScoreComponents =>
    [
        ("interest", L.ScoreComponents.Interest, false, s => s.Interest),
        ("novelty", L.ScoreComponents.Novelty, false, s => s.Novelty),
        ("context", L.ScoreComponents.Context, false, s => s.Context),
        ("goalFit", L.ScoreComponents.GoalFit, false, s => s.GoalFit),
        ("diversity", L.ScoreComponents.Diversity, false, s => s.Diversity),
        ("feedbackFit", L.ScoreComponents.FeedbackFit, false, s => s.FeedbackFit),
        ("repetition", L.ScoreComponents.Repetition, true, s => s.Repetition),
        ("friction", L.ScoreComponents.Friction, true, s => s.Friction),
        ("risk", L.ScoreComponents.Risk, true, s => s.Risk)
    ];

    /// <summary>Hava ikonu WMO koduna göre (web weather-chip ile aynı).</summary>
    public static string WeatherIcon(int code) => code >= 51 ? "cloud-rain" : code == 0 ? "sun" : "cloud";
}
