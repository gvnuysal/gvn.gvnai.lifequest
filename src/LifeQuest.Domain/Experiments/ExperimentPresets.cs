using LifeQuest.Domain.Localization;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Experiments;

/// <summary>Yönetim panelinde tek tıkla taslağa dönüşen hazır deney.</summary>
/// <summary>Ad ve hipotez iki dilde; <see cref="Name"/>/<see cref="Hypothesis"/> o anki dilde.</summary>
public sealed record ExperimentPreset(
    string Key,
    LocalizedText NameText,
    LocalizedText HypothesisText,
    IReadOnlyDictionary<string, double> Overrides,
    double TreatmentShare,
    LocalizedText SourceText)
{
    public string Name => NameText.Current;
    public string Hypothesis => HypothesisText.Current;
    public string Source => SourceText.Current;
}

/// <summary>
/// Offline simülasyonun (docs/simulasyon-raporu.md, "Öneriler") önerdiği ilk A/B deneyleri, öncelik sırasıyla.
/// Birincil metrik north-star; koruma metriği "ilgimi çekmedi" oranı (<c>ExperimentStatistics.GuardrailBreached</c>).
/// </summary>
public static class ExperimentPresets
{
    private static readonly LocalizedText SimulationSource = new("Simülasyon raporu · Öneri 2", "Simulation report · Recommendation 2");

    public static readonly IReadOnlyList<ExperimentPreset> All =
    [
        new("loved-repeat-0.8",
            new("Sevdiğini tekrarla 0,8", "Repeat what you loved 0.8"),
            new("5 puan verilen deneyimleri cooldown sonrası daha kolay tekrar önermek north-star'ı artırır. " +
                "Simülasyonda 0,6 → 0,8: north-star 6,99 → 7,12, keşif değişmedi.",
                "Re-suggesting 5-star experiences more easily after their cooldown raises the north star. " +
                "In the simulation 0.6 → 0.8: north star 6.99 → 7.12, discovery unchanged."),
            new Dictionary<string, double> { [nameof(RecommendationWeights.LovedRepeatNovelty)] = 0.8 },
            0.5,
            SimulationSource),
        new("chill-exploration-0.1",
            new("Sakin modda keşif %10", "Chill mode discovery 10%"),
            new("Derin katalogla Sakin modda keşif slotunun kazancı küçüldü; %20 yerine %10 north-star'ı artırır, " +
                "gizli ilgi keşfini belirgin düşürmez.",
                "With the deeper catalog, the discovery slot's gain in Chill mode shrank; 10% instead of 20% raises the north star " +
                "without noticeably lowering hidden-interest discovery."),
            new Dictionary<string, double> { [nameof(RecommendationWeights.ExplorationRateChill)] = 0.1 },
            0.5,
            SimulationSource),
        new("surprise-novelty-0.20",
            new("Şaşırt Beni yeniliği 0,20", "Surprise Me novelty 0.20"),
            new("Şaşırt Beni'de yenilik ağırlığını 0,25'ten 0,20'ye indirmek isabeti artırır; simülasyonda gizli ilgi " +
                "keşfi %80'in üzerinde kaldı.",
                "Lowering the Surprise Me novelty weight from 0.25 to 0.20 improves accuracy; in the simulation, hidden-interest " +
                "discovery stayed above 80%."),
            new Dictionary<string, double> { [nameof(RecommendationWeights.NoveltySurpriseMe)] = 0.20 },
            0.5,
            SimulationSource)
    ];

    public static ExperimentPreset? Find(string key) => All.FirstOrDefault(p => p.Key == key);
}
