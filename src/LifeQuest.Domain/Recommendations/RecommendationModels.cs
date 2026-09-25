using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Domain.Recommendations;

/// <summary>Engine'e giren, önerilebilir (aktif + güvenli) template'in salt-okunur projeksiyonu.</summary>
public sealed record QuestCandidate(
    Guid TemplateId,
    string Code,
    int Version,
    string Title,
    string Description,
    QuestType Type,
    Difficulty Difficulty,
    LifeCategory Category,
    LifeCategory? SecondaryCategory,
    int MinMinutes,
    int MaxMinutes,
    CostBand Cost,
    DayPart DayParts,
    bool RequiresCity,
    double RiskScore,
    int CooldownDays,
    IReadOnlyList<Guid> InterestIds,
    bool IsOutdoor = false,
    PhysicalEffort Effort = PhysicalEffort.Light);

public sealed record RecommendationProfile(
    DiscoveryRadius Radius,
    CostBand Budget,
    int WeeklyAvailableMinutes,
    IReadOnlySet<LifeCategory> Goals,
    IReadOnlyDictionary<Guid, double> InterestWeights,
    bool HasCity,
    PhysicalEffort MaxEffort = PhysicalEffort.Vigorous);

/// <param name="LocalNow">Kullanıcının saat dilimindeki an (gün dilimi / hafta sonu için).</param>
/// <param name="UtcNow">Cooldown ve pencere hesapları için.</param>
/// <param name="Seed">Keşif slotu seçimi için deterministik tohum (kullanıcı + gün).</param>
/// <param name="AvailableMinutes">"Bu akşam 2 saatim var" gibi bağlamsal süre.</param>
/// <param name="MaxCost">Bağlamsal bütçe; verilmezse profil bütçesi kullanılır.</param>
/// <param name="RequireShortQuest">İlk slota mümkünse kısa (Daily) bir quest koy: ilk 5 dakikada uygulanabilir bir görev.</param>
public sealed record RecommendationContext(
    DateTime LocalNow,
    DateTime UtcNow,
    int Count,
    int Seed,
    int? AvailableMinutes = null,
    CostBand? MaxCost = null,
    bool RequireShortQuest = true);

public sealed record QuestHistoryItem(
    Guid TemplateId,
    LifeCategory Category,
    IReadOnlyList<Guid> InterestIds,
    QuestStatus Status,
    DateTime OfferedAt,
    DateTime? ResolvedAt,
    SkipReason? SkipReason,
    int? Rating,
    FeedbackPreference? Preference)
{
    public DateTime LastActivityAt => ResolvedAt ?? OfferedAt;
}

public enum ReasonCode
{
    ExplorationPick,
    Diversification,
    AdjacentInterest,
    NewCategory,
    InterestMatch,
    GoalFit,
    FitsAvailableTime,
    Free,
    LovedBefore
}

public sealed record RecommendationReason(ReasonCode Code, string Text);

public sealed record RecommendedQuest(
    QuestCandidate Candidate,
    ScoreBreakdown Score,
    bool IsExploration,
    IReadOnlyList<RecommendationReason> Reasons,
    string Explanation);

public sealed record RecommendationResult(
    IReadOnlyList<RecommendedQuest> Items,
    int CandidateCount,
    int EligibleCount,
    IReadOnlyDictionary<string, int> FilteredOut)
{
    public bool IsEmpty => Items.Count == 0;
}
