using LifeQuest.Domain.Localization;

namespace LifeQuest.Domain.Recommendations;

/// <summary>
/// Admin panelinden değiştirilebilen ağırlıkların tek kaynağı: anahtar, sınır ve açıklama. Listede olmayan alanlar
/// (örn. <see cref="RecommendationWeights.GuidedExploration"/>) yalnızca konfigürasyondan değişir.
/// </summary>
public static class RecommendationWeightCatalog
{
    public static readonly IReadOnlyList<WeightField> Fields =
    [
        Weight(nameof(RecommendationWeights.InterestChill), new("İlgi · Sakin", "Interest · Chill"), WeightGroup.Interest,
            new("Sakin modda kullanıcının ilgi alanlarına uyumun ağırlığı.", "Weight of matching the user's interests in Chill mode."), w => w.InterestChill, (w, v) => w with { InterestChill = v }),
        Weight(nameof(RecommendationWeights.InterestExplore), new("İlgi · Dengeli", "Interest · Balanced"), WeightGroup.Interest,
            new("Dengeli modda ilgi uyumunun ağırlığı.", "Weight of interest match in Balanced mode."), w => w.InterestExplore, (w, v) => w with { InterestExplore = v }),
        Weight(nameof(RecommendationWeights.InterestSurpriseMe), new("İlgi · Şaşırt Beni", "Interest · Surprise Me"), WeightGroup.Interest,
            new("Şaşırt Beni modunda ilgi uyumunun ağırlığı.", "Weight of interest match in Surprise Me mode."), w => w.InterestSurpriseMe, (w, v) => w with { InterestSurpriseMe = v }),

        Weight(nameof(RecommendationWeights.NoveltyChill), new("Yenilik · Sakin", "Novelty · Chill"), WeightGroup.Novelty,
            new("Sakin modda daha önce denenmemiş alanlara verilen ağırlık.", "Weight given to untried areas in Chill mode."), w => w.NoveltyChill, (w, v) => w with { NoveltyChill = v }),
        Weight(nameof(RecommendationWeights.NoveltyExplore), new("Yenilik · Dengeli", "Novelty · Balanced"), WeightGroup.Novelty,
            new("Dengeli modda yenilik ağırlığı.", "Novelty weight in Balanced mode."), w => w.NoveltyExplore, (w, v) => w with { NoveltyExplore = v }),
        Weight(nameof(RecommendationWeights.NoveltySurpriseMe), new("Yenilik · Şaşırt Beni", "Novelty · Surprise Me"), WeightGroup.Novelty,
            new("Şaşırt Beni modunda yenilik ağırlığı. Simülasyonda 0,25'in altında keşif düşüyor.", "Novelty weight in Surprise Me mode. In the simulation, discovery drops below 0.25."), w => w.NoveltySurpriseMe, (w, v) => w with { NoveltySurpriseMe = v }),

        Weight(nameof(RecommendationWeights.Context), new("Bağlam uyumu", "Context fit"), WeightGroup.Score,
            new("Gün dilimi, süre ve bütçe uyumu.", "Fit with time of day, duration and budget."), w => w.Context, (w, v) => w with { Context = v }),
        Weight(nameof(RecommendationWeights.LocalEventBoost), new("Şehirde etkinlik", "Event in the city"), WeightGroup.Score,
            new("Kullanıcının şehrinde bu görevle bağlantılı yaklaşan bir etkinlik varsa bağlam skoruna eklenen pay.", "Share added to the context score when there is an upcoming event linked to this quest in the user's city."),
            w => w.LocalEventBoost, (w, v) => w with { LocalEventBoost = v }),
        Weight(nameof(RecommendationWeights.GoodWeatherOutdoorBoost), new("Güzel havada açık hava", "Outdoors in good weather"), WeightGroup.Score,
            new("Hava uygunken gündüz açık hava görevine eklenen bağlam payı. Kötü havada açık hava görevleri zaten o an önerilmez.", "Context share added to an outdoor quest during the day when the weather is right. In bad weather, outdoor quests aren't suggested for right now anyway."),
            w => w.GoodWeatherOutdoorBoost, (w, v) => w with { GoodWeatherOutdoorBoost = v }),
        Weight(nameof(RecommendationWeights.GoalFit), new("Hedef uyumu", "Goal fit"), WeightGroup.Score,
            new("Kullanıcının seçtiği hedef kategorilere uyum.", "Fit with the goal categories the user chose."), w => w.GoalFit, (w, v) => w with { GoalFit = v }),
        Weight(nameof(RecommendationWeights.Diversity), new("Çeşitlilik", "Variety"), WeightGroup.Score,
            new("Günlük listede farklı kategorilere verilen ödül.", "Reward for different categories in the daily list."), w => w.Diversity, (w, v) => w with { Diversity = v }),
        Weight(nameof(RecommendationWeights.FeedbackFit), new("Geri bildirim uyumu", "Feedback fit"), WeightGroup.Score,
            new("\"Daha fazla/daha az\" tercihlerinin etkisi.", "Effect of \"more/less like this\" preferences."), w => w.FeedbackFit, (w, v) => w with { FeedbackFit = v }),

        Weight(nameof(RecommendationWeights.Repetition), new("Tekrar cezası", "Repetition penalty"), WeightGroup.Penalty,
            new("Yakın zamanda yapılan veya gösterilen şeylerin cezası.", "Penalty for things done or shown recently."), w => w.Repetition, (w, v) => w with { Repetition = v }),
        Weight(nameof(RecommendationWeights.Friction), new("Sürtünme cezası", "Friction penalty"), WeightGroup.Penalty,
            new("Pahalı / zamanım yok / uzak geri bildirimlerine benzeyen önerilerin cezası.", "Penalty for suggestions similar to \"too expensive / no time / too far\" feedback."), w => w.Friction, (w, v) => w with { Friction = v }),
        Weight(nameof(RecommendationWeights.Risk), new("Risk cezası", "Risk penalty"), WeightGroup.Penalty,
            new("Template risk skorunun cezası.", "Penalty from the template risk score."), w => w.Risk, (w, v) => w with { Risk = v }),
        Weight(nameof(RecommendationWeights.IgnoredOfferPenalty), new("Görmezden gelinen öneri", "Ignored suggestion"), WeightGroup.Penalty,
            new("Gösterilip seçilmeyen her öneri için ek tekrar cezası.", "Extra repetition penalty for each suggestion shown but not picked."), w => w.IgnoredOfferPenalty, (w, v) => w with { IgnoredOfferPenalty = v }),

        Weight(nameof(RecommendationWeights.AdjacencyFactor), new("Komşu ilgi çarpanı", "Adjacent interest factor"), WeightGroup.TasteGraph,
            new("Taste Graph komşusu üzerinden gelen ilginin doğrudan ilgiye oranı.", "Ratio of interest coming via a Taste Graph neighbor to direct interest."), w => w.AdjacencyFactor, (w, v) => w with { AdjacencyFactor = v }),
        Weight(nameof(RecommendationWeights.BaselineInterest), new("Taban ilgi", "Baseline interest"), WeightGroup.TasteGraph,
            new("Hiçbir ilgiyle eşleşmeyen quest'in ilgi skoru.", "Interest score of a quest that matches no interest."), w => w.BaselineInterest, (w, v) => w with { BaselineInterest = v }, max: 0.5),

        Weight(nameof(RecommendationWeights.ExplorationRateChill), new("Keşif oranı · Sakin", "Discovery rate · Chill"), WeightGroup.Exploration,
            new("Sakin modda günlük keşif slotunun açılma olasılığı (yalnızca komşu ilgiler).", "Chance the daily discovery slot opens in Chill mode (adjacent interests only)."), w => w.ExplorationRateChill, (w, v) => w with { ExplorationRateChill = v }),
        Weight(nameof(RecommendationWeights.ExplorationRateExplore), new("Keşif oranı · Dengeli", "Discovery rate · Balanced"), WeightGroup.Exploration,
            new("Dengeli modda keşif slotunun açılma olasılığı.", "Chance the discovery slot opens in Balanced mode."), w => w.ExplorationRateExplore, (w, v) => w with { ExplorationRateExplore = v }),
        Weight(nameof(RecommendationWeights.ExplorationRateSurpriseMe), new("Keşif oranı · Şaşırt Beni", "Discovery rate · Surprise Me"), WeightGroup.Exploration,
            new("Şaşırt Beni modunda keşif slotunun açılma olasılığı.", "Chance the discovery slot opens in Surprise Me mode."), w => w.ExplorationRateSurpriseMe, (w, v) => w with { ExplorationRateSurpriseMe = v }),
        Weight(nameof(RecommendationWeights.ExplorationMaxInterest), new("Keşif · azami ilgi", "Discovery · max interest"), WeightGroup.Exploration,
            new("Keşif adayının ilgi skoru bunun altında olmalı.", "A discovery candidate's interest score must be below this."), w => w.ExplorationMaxInterest, (w, v) => w with { ExplorationMaxInterest = v }),
        Weight(nameof(RecommendationWeights.ExplorationMinNovelty), new("Keşif · asgari yenilik", "Discovery · min novelty"), WeightGroup.Exploration,
            new("Keşif adayının yenilik skoru bunun üstünde olmalı.", "A discovery candidate's novelty score must be above this."), w => w.ExplorationMinNovelty, (w, v) => w with { ExplorationMinNovelty = v }),
        Weight(nameof(RecommendationWeights.LovedRepeatNovelty), new("Sevdiğini tekrarla", "Repeat what you loved"), WeightGroup.Novelty,
            new("5 puan verilen bir deneyim cooldown'dan sonra bu yenilik skoruyla yeniden önerilebilir (0,2 = kapalı).", "A 5-star experience can be suggested again after its cooldown with this novelty score (0.2 = off)."),
            w => w.LovedRepeatNovelty, (w, v) => w with { LovedRepeatNovelty = v }),

        Days(nameof(RecommendationWeights.RecentWindowDays), new("Yakın geçmiş penceresi", "Recent history window"),
            new("Tekrar ve çeşitlilik hesabında bakılan gün sayısı.", "Days looked back when computing repetition and variety."), w => w.RecentWindowDays, (w, v) => w with { RecentWindowDays = v }, 1, 30),
        Days(nameof(RecommendationWeights.IgnoredOfferWindowDays), new("Görmezden gelme penceresi", "Ignoring window"),
            new("Görmezden gelinen önerilerin sayıldığı gün sayısı.", "Days in which ignored suggestions are counted."), w => w.IgnoredOfferWindowDays, (w, v) => w with { IgnoredOfferWindowDays = v }, 1, 30),
        Days(nameof(RecommendationWeights.NotInterestedBlockDays), new("\"İlgimi çekmedi\" engeli", "\"Not for me\" block"),
            new("\"İlgimi çekmedi\" denen template'in önerilmediği gün sayısı.", "Days a template marked \"not for me\" isn't suggested."), w => w.NotInterestedBlockDays, (w, v) => w with { NotInterestedBlockDays = v }, 1, 90),
        Days(nameof(RecommendationWeights.LessLikeThisBlockDays), new("\"Daha az\" engeli", "\"Less like this\" block"),
            new("\"Bundan daha az\" denen template'in önerilmediği gün sayısı.", "Days a template marked \"less like this\" isn't suggested."), w => w.LessLikeThisBlockDays, (w, v) => w with { LessLikeThisBlockDays = v }, 1, 180)
    ];

    private static readonly Dictionary<string, WeightField> ByKey = Fields.ToDictionary(f => f.Key, StringComparer.Ordinal);

    public static bool TryGet(string key, out WeightField field) => ByKey.TryGetValue(key, out field!);

    public static WeightField Get(string key)
        => ByKey.TryGetValue(key, out var field) ? field : throw new ArgumentException($"Bilinmeyen ağırlık: {key}", nameof(key));

    /// <returns>Anahtar → hata mesajı. Boşsa değerler geçerli.</returns>
    public static IReadOnlyDictionary<string, string> Validate(IReadOnlyDictionary<string, double> values)
    {
        var errors = new Dictionary<string, string>();
        foreach (var (key, value) in values)
        {
            if (!ByKey.TryGetValue(key, out var field))
                errors[key] = Text.Of("Bu ağırlık panelden değiştirilemez.", "This weight can't be changed from the panel.");
            else if (double.IsNaN(value) || value < field.Min || value > field.Max)
                errors[key] = Text.Of($"{field.Label} {field.Min}–{field.Max} arasında olmalıdır.", $"{field.Label} must be between {field.Min} and {field.Max}.");
            else if (field.IsInteger && Math.Abs(value - Math.Round(value)) > 1e-9)
                errors[key] = Text.Of($"{field.Label} tam sayı olmalıdır.", $"{field.Label} must be a whole number.");
        }

        return errors;
    }

    /// <summary>
    /// Override'ları konfigürasyon değerlerinin üzerine uygular. Saklanmış ama artık geçersiz bir değer (katalog
    /// değiştiyse) sessizce atlanır; motor hiçbir zaman sınır dışı bir ağırlıkla çalışmaz.
    /// </summary>
    public static RecommendationWeights Apply(RecommendationWeights defaults, IReadOnlyDictionary<string, double> overrides)
    {
        var weights = defaults;
        foreach (var (key, value) in overrides)
        {
            if (ByKey.TryGetValue(key, out var field) && value >= field.Min && value <= field.Max)
                weights = field.Set(weights, field.Normalize(value));
        }

        return weights;
    }

    private static WeightField Weight(
        string key, LocalizedText label, WeightGroup group, LocalizedText description,
        Func<RecommendationWeights, double> get, Func<RecommendationWeights, double, RecommendationWeights> set,
        double max = 1)
        => new(key, label, group, description, 0, max, 0.01, false, get, set);

    private static WeightField Days(
        string key, LocalizedText label, LocalizedText description,
        Func<RecommendationWeights, int> get, Func<RecommendationWeights, int, RecommendationWeights> set, int min, int max)
        => new(key, label, WeightGroup.Windows, description, min, max, 1, true, w => get(w), (w, v) => set(w, (int)Math.Round(v)));
}

/// <summary><see cref="Label"/> ve <see cref="Description"/> o anki dilde (yönetim paneli iki dilli).</summary>
public sealed record WeightField(
    string Key,
    LocalizedText LabelText,
    WeightGroup Group,
    LocalizedText DescriptionText,
    double Min,
    double Max,
    double Step,
    bool IsInteger,
    Func<RecommendationWeights, double> Get,
    Func<RecommendationWeights, double, RecommendationWeights> Set)
{
    public string Label => LabelText.Current;
    public string Description => DescriptionText.Current;

    /// <summary>Saklamadan önce yuvarlar: tam sayılar tam, ağırlıklar 3 basamak.</summary>
    public double Normalize(double value) => IsInteger ? Math.Round(value) : Math.Round(value, 3);
}

public enum WeightGroup
{
    Interest = 1,
    Novelty = 2,
    Score = 3,
    Penalty = 4,
    TasteGraph = 5,
    Exploration = 6,
    Windows = 7
}
