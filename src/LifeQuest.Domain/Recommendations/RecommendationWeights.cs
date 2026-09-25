using LifeQuest.Domain.Profiles;

namespace LifeQuest.Domain.Recommendations;

/// <summary>Skor ağırlıkları konfigürasyondan yönetilir (appsettings: Recommendation).</summary>
public sealed record RecommendationWeights
{
    public const string SectionName = "Recommendation";

    public double InterestChill { get; init; } = 0.40;
    public double InterestExplore { get; init; } = 0.30;
    public double InterestSurpriseMe { get; init; } = 0.20;

    public double NoveltyChill { get; init; } = 0.05;
    public double NoveltyExplore { get; init; } = 0.20;
    public double NoveltySurpriseMe { get; init; } = 0.25;

    public double Context { get; init; } = 0.15;
    public double GoalFit { get; init; } = 0.15;
    public double Diversity { get; init; } = 0.10;
    public double FeedbackFit { get; init; } = 0.10;
    public double Repetition { get; init; } = 0.25;
    public double Friction { get; init; } = 0.15;
    public double Risk { get; init; } = 0.30;

    /// <summary>Taste Graph komşusu üzerinden gelen ilginin doğrudan ilgiye göre çarpanı.</summary>
    public double AdjacencyFactor { get; init; } = 0.6;

    /// <summary>Hiçbir ilgiyle eşleşmeyen quest için taban ilgi skoru.</summary>
    public double BaselineInterest { get; init; } = 0.1;

    public int RecentWindowDays { get; init; } = 7;

    /// <summary>Kabul edilmeden süresi dolan öneri başına tekrar cezası ve bakılan pencere.</summary>
    public double IgnoredOfferPenalty { get; init; } = 0.3;
    public int IgnoredOfferWindowDays { get; init; } = 7;
    public int NotInterestedBlockDays { get; init; } = 14;
    public int LessLikeThisBlockDays { get; init; } = 30;

    /// <summary>Keşif slotu adayları: ilgi skoru bunun altında ve novelty bunun üstünde olanlar.</summary>
    public double ExplorationMaxInterest { get; init; } = 0.5;
    public double ExplorationMinNovelty { get; init; } = 0.6;

    public double InterestWeightFor(DiscoveryRadius radius) => radius switch
    {
        DiscoveryRadius.Chill => InterestChill,
        DiscoveryRadius.SurpriseMe => InterestSurpriseMe,
        _ => InterestExplore
    };

    public double NoveltyWeightFor(DiscoveryRadius radius) => radius switch
    {
        DiscoveryRadius.Chill => NoveltyChill,
        DiscoveryRadius.SurpriseMe => NoveltySurpriseMe,
        _ => NoveltyExplore
    };

    /// <summary>
    /// Keşif slotunun açılma olasılığı (günlük, tohumlu). Chill'de yalnızca Taste Graph komşusu ve risksiz
    /// adaylarla hafif keşif yapılır; aday yoksa slot normal öneriye döner.
    /// </summary>
    public double ExplorationRateChill { get; init; } = 0.2;
    public double ExplorationRateExplore { get; init; } = 0.5;

    /// <summary>Keşif slotu önce Taste Graph komşularını dener. <c>false</c> yalnızca karşılaştırma içindir (ilk sürüm).</summary>
    public bool GuidedExploration { get; init; } = true;
    public double ExplorationRateSurpriseMe { get; init; } = 1.0;

    /// <summary>Contextual bandit'e geçmeden önce kontrollü keşif: günde en fazla 1 keşif slotu.</summary>
    public static int ExplorationSlotsFor(int count) => count < 2 ? 0 : 1;

    public double ExplorationRateFor(DiscoveryRadius radius) => radius switch
    {
        DiscoveryRadius.Chill => ExplorationRateChill,
        DiscoveryRadius.SurpriseMe => ExplorationRateSurpriseMe,
        _ => ExplorationRateExplore
    };
}
