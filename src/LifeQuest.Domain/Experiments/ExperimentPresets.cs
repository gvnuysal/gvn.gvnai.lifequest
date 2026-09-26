using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Experiments;

/// <summary>Yönetim panelinde tek tıkla taslağa dönüşen hazır deney.</summary>
public sealed record ExperimentPreset(
    string Key,
    string Name,
    string Hypothesis,
    IReadOnlyDictionary<string, double> Overrides,
    double TreatmentShare,
    string Source);

/// <summary>
/// Offline simülasyonun (docs/simulasyon-raporu.md, "Öneriler") önerdiği ilk A/B deneyleri, öncelik sırasıyla.
/// Birincil metrik north-star; koruma metriği "ilgimi çekmedi" oranı (<c>ExperimentStatistics.GuardrailBreached</c>).
/// </summary>
public static class ExperimentPresets
{
    private const string SimulationSource = "Simülasyon raporu · Öneri 2";

    public static readonly IReadOnlyList<ExperimentPreset> All =
    [
        new("loved-repeat-0.8",
            "Sevdiğini tekrarla 0,8",
            "5 puan verilen deneyimleri cooldown sonrası daha kolay tekrar önermek north-star'ı artırır. " +
            "Simülasyonda 0,6 → 0,8: north-star 6,99 → 7,12, keşif değişmedi.",
            new Dictionary<string, double> { [nameof(RecommendationWeights.LovedRepeatNovelty)] = 0.8 },
            0.5,
            SimulationSource),
        new("chill-exploration-0.1",
            "Sakin modda keşif %10",
            "Derin katalogla Sakin modda keşif slotunun kazancı küçüldü; %20 yerine %10 north-star'ı artırır, " +
            "gizli ilgi keşfini belirgin düşürmez.",
            new Dictionary<string, double> { [nameof(RecommendationWeights.ExplorationRateChill)] = 0.1 },
            0.5,
            SimulationSource),
        new("surprise-novelty-0.20",
            "Şaşırt Beni yeniliği 0,20",
            "Şaşırt Beni'de yenilik ağırlığını 0,25'ten 0,20'ye indirmek isabeti artırır; simülasyonda gizli ilgi " +
            "keşfi %80'in üzerinde kaldı.",
            new Dictionary<string, double> { [nameof(RecommendationWeights.NoveltySurpriseMe)] = 0.20 },
            0.5,
            SimulationSource)
    ];

    public static ExperimentPreset? Find(string key) => All.FirstOrDefault(p => p.Key == key);
}
