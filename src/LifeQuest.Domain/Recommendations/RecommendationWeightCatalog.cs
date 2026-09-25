namespace LifeQuest.Domain.Recommendations;

/// <summary>
/// Admin panelinden değiştirilebilen ağırlıkların tek kaynağı: anahtar, sınır ve açıklama. Listede olmayan alanlar
/// (örn. <see cref="RecommendationWeights.GuidedExploration"/>) yalnızca konfigürasyondan değişir.
/// </summary>
public static class RecommendationWeightCatalog
{
    public static readonly IReadOnlyList<WeightField> Fields =
    [
        Weight(nameof(RecommendationWeights.InterestChill), "İlgi · Sakin", WeightGroup.Interest,
            "Sakin modda kullanıcının ilgi alanlarına uyumun ağırlığı.", w => w.InterestChill, (w, v) => w with { InterestChill = v }),
        Weight(nameof(RecommendationWeights.InterestExplore), "İlgi · Dengeli", WeightGroup.Interest,
            "Dengeli modda ilgi uyumunun ağırlığı.", w => w.InterestExplore, (w, v) => w with { InterestExplore = v }),
        Weight(nameof(RecommendationWeights.InterestSurpriseMe), "İlgi · Şaşırt Beni", WeightGroup.Interest,
            "Şaşırt Beni modunda ilgi uyumunun ağırlığı.", w => w.InterestSurpriseMe, (w, v) => w with { InterestSurpriseMe = v }),

        Weight(nameof(RecommendationWeights.NoveltyChill), "Yenilik · Sakin", WeightGroup.Novelty,
            "Sakin modda daha önce denenmemiş alanlara verilen ağırlık.", w => w.NoveltyChill, (w, v) => w with { NoveltyChill = v }),
        Weight(nameof(RecommendationWeights.NoveltyExplore), "Yenilik · Dengeli", WeightGroup.Novelty,
            "Dengeli modda yenilik ağırlığı.", w => w.NoveltyExplore, (w, v) => w with { NoveltyExplore = v }),
        Weight(nameof(RecommendationWeights.NoveltySurpriseMe), "Yenilik · Şaşırt Beni", WeightGroup.Novelty,
            "Şaşırt Beni modunda yenilik ağırlığı. Simülasyonda 0,25'in altında keşif düşüyor.", w => w.NoveltySurpriseMe, (w, v) => w with { NoveltySurpriseMe = v }),

        Weight(nameof(RecommendationWeights.Context), "Bağlam uyumu", WeightGroup.Score,
            "Gün dilimi, süre ve bütçe uyumu.", w => w.Context, (w, v) => w with { Context = v }),
        Weight(nameof(RecommendationWeights.GoalFit), "Hedef uyumu", WeightGroup.Score,
            "Kullanıcının seçtiği hedef kategorilere uyum.", w => w.GoalFit, (w, v) => w with { GoalFit = v }),
        Weight(nameof(RecommendationWeights.Diversity), "Çeşitlilik", WeightGroup.Score,
            "Günlük listede farklı kategorilere verilen ödül.", w => w.Diversity, (w, v) => w with { Diversity = v }),
        Weight(nameof(RecommendationWeights.FeedbackFit), "Geri bildirim uyumu", WeightGroup.Score,
            "\"Daha fazla/daha az\" tercihlerinin etkisi.", w => w.FeedbackFit, (w, v) => w with { FeedbackFit = v }),

        Weight(nameof(RecommendationWeights.Repetition), "Tekrar cezası", WeightGroup.Penalty,
            "Yakın zamanda yapılan veya gösterilen şeylerin cezası.", w => w.Repetition, (w, v) => w with { Repetition = v }),
        Weight(nameof(RecommendationWeights.Friction), "Sürtünme cezası", WeightGroup.Penalty,
            "Pahalı / zamanım yok / uzak geri bildirimlerine benzeyen önerilerin cezası.", w => w.Friction, (w, v) => w with { Friction = v }),
        Weight(nameof(RecommendationWeights.Risk), "Risk cezası", WeightGroup.Penalty,
            "Template risk skorunun cezası.", w => w.Risk, (w, v) => w with { Risk = v }),
        Weight(nameof(RecommendationWeights.IgnoredOfferPenalty), "Görmezden gelinen öneri", WeightGroup.Penalty,
            "Gösterilip seçilmeyen her öneri için ek tekrar cezası.", w => w.IgnoredOfferPenalty, (w, v) => w with { IgnoredOfferPenalty = v }),

        Weight(nameof(RecommendationWeights.AdjacencyFactor), "Komşu ilgi çarpanı", WeightGroup.TasteGraph,
            "Taste Graph komşusu üzerinden gelen ilginin doğrudan ilgiye oranı.", w => w.AdjacencyFactor, (w, v) => w with { AdjacencyFactor = v }),
        Weight(nameof(RecommendationWeights.BaselineInterest), "Taban ilgi", WeightGroup.TasteGraph,
            "Hiçbir ilgiyle eşleşmeyen quest'in ilgi skoru.", w => w.BaselineInterest, (w, v) => w with { BaselineInterest = v }, max: 0.5),

        Weight(nameof(RecommendationWeights.ExplorationRateChill), "Keşif oranı · Sakin", WeightGroup.Exploration,
            "Sakin modda günlük keşif slotunun açılma olasılığı (yalnızca komşu ilgiler).", w => w.ExplorationRateChill, (w, v) => w with { ExplorationRateChill = v }),
        Weight(nameof(RecommendationWeights.ExplorationRateExplore), "Keşif oranı · Dengeli", WeightGroup.Exploration,
            "Dengeli modda keşif slotunun açılma olasılığı.", w => w.ExplorationRateExplore, (w, v) => w with { ExplorationRateExplore = v }),
        Weight(nameof(RecommendationWeights.ExplorationRateSurpriseMe), "Keşif oranı · Şaşırt Beni", WeightGroup.Exploration,
            "Şaşırt Beni modunda keşif slotunun açılma olasılığı.", w => w.ExplorationRateSurpriseMe, (w, v) => w with { ExplorationRateSurpriseMe = v }),
        Weight(nameof(RecommendationWeights.ExplorationMaxInterest), "Keşif · azami ilgi", WeightGroup.Exploration,
            "Keşif adayının ilgi skoru bunun altında olmalı.", w => w.ExplorationMaxInterest, (w, v) => w with { ExplorationMaxInterest = v }),
        Weight(nameof(RecommendationWeights.ExplorationMinNovelty), "Keşif · asgari yenilik", WeightGroup.Exploration,
            "Keşif adayının yenilik skoru bunun üstünde olmalı.", w => w.ExplorationMinNovelty, (w, v) => w with { ExplorationMinNovelty = v }),
        Weight(nameof(RecommendationWeights.LovedRepeatNovelty), "Sevdiğini tekrarla", WeightGroup.Novelty,
            "5 puan verilen bir deneyim cooldown'dan sonra bu yenilik skoruyla yeniden önerilebilir (0,2 = kapalı).",
            w => w.LovedRepeatNovelty, (w, v) => w with { LovedRepeatNovelty = v }),

        Days(nameof(RecommendationWeights.RecentWindowDays), "Yakın geçmiş penceresi",
            "Tekrar ve çeşitlilik hesabında bakılan gün sayısı.", w => w.RecentWindowDays, (w, v) => w with { RecentWindowDays = v }, 1, 30),
        Days(nameof(RecommendationWeights.IgnoredOfferWindowDays), "Görmezden gelme penceresi",
            "Görmezden gelinen önerilerin sayıldığı gün sayısı.", w => w.IgnoredOfferWindowDays, (w, v) => w with { IgnoredOfferWindowDays = v }, 1, 30),
        Days(nameof(RecommendationWeights.NotInterestedBlockDays), "\"İlgimi çekmedi\" engeli",
            "\"İlgimi çekmedi\" denen template'in önerilmediği gün sayısı.", w => w.NotInterestedBlockDays, (w, v) => w with { NotInterestedBlockDays = v }, 1, 90),
        Days(nameof(RecommendationWeights.LessLikeThisBlockDays), "\"Daha az\" engeli",
            "\"Bundan daha az\" denen template'in önerilmediği gün sayısı.", w => w.LessLikeThisBlockDays, (w, v) => w with { LessLikeThisBlockDays = v }, 1, 180)
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
                errors[key] = "Bu ağırlık panelden değiştirilemez.";
            else if (double.IsNaN(value) || value < field.Min || value > field.Max)
                errors[key] = $"{field.Label} {field.Min}–{field.Max} arasında olmalıdır.";
            else if (field.IsInteger && Math.Abs(value - Math.Round(value)) > 1e-9)
                errors[key] = $"{field.Label} tam sayı olmalıdır.";
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
        string key, string label, WeightGroup group, string description,
        Func<RecommendationWeights, double> get, Func<RecommendationWeights, double, RecommendationWeights> set,
        double max = 1)
        => new(key, label, group, description, 0, max, 0.01, false, get, set);

    private static WeightField Days(
        string key, string label, string description,
        Func<RecommendationWeights, int> get, Func<RecommendationWeights, int, RecommendationWeights> set, int min, int max)
        => new(key, label, WeightGroup.Windows, description, min, max, 1, true, w => get(w), (w, v) => set(w, (int)Math.Round(v)));
}

public sealed record WeightField(
    string Key,
    string Label,
    WeightGroup Group,
    string Description,
    double Min,
    double Max,
    double Step,
    bool IsInteger,
    Func<RecommendationWeights, double> Get,
    Func<RecommendationWeights, double, RecommendationWeights> Set)
{
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
