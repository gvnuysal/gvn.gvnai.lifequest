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
    public double NoveltySurpriseMe { get; init; } = 0.35;

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

    /// <summary>Contextual bandit'e geçmeden önce kontrollü keşif: Chill'de 0, diğerlerinde 1 keşif slotu.</summary>
    public static int ExplorationSlotsFor(DiscoveryRadius radius, int count)
        => radius == DiscoveryRadius.Chill || count < 2 ? 0 : 1;
}
