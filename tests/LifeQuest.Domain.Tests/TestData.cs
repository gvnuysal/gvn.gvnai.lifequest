using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Tests;

internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 9, 23, 16, 0, 0, DateTimeKind.Utc); // Çarşamba
    public static readonly DateTime LocalEvening = new(2026, 9, 23, 19, 0, 0);

    public static QuestCandidate Candidate(
        string code,
        LifeCategory category,
        IReadOnlyList<Guid> interests,
        QuestType type = QuestType.Weekly,
        Difficulty difficulty = Difficulty.Medium,
        LifeCategory? secondary = null,
        int minMinutes = 45,
        int maxMinutes = 90,
        CostBand cost = CostBand.Low,
        DayPart dayParts = DayPart.Any,
        bool requiresCity = false,
        double risk = 0,
        int cooldownDays = 14)
        => new(Guid.NewGuid(), code, 1, code, code, type, difficulty, category, secondary,
            minMinutes, maxMinutes, cost, dayParts, requiresCity, risk, cooldownDays, interests);

    public static RecommendationProfile Profile(
        Dictionary<Guid, double> interests,
        DiscoveryRadius radius = DiscoveryRadius.Explore,
        CostBand budget = CostBand.Medium,
        int weeklyMinutes = 600,
        IReadOnlySet<LifeCategory>? goals = null,
        bool hasCity = true)
        => new(radius, budget, weeklyMinutes, goals ?? new HashSet<LifeCategory>(), interests, hasCity);

    public static RecommendationContext Context(int count = 3, int? availableMinutes = null, bool requireShort = false, int seed = 42)
        => new(LocalEvening, UtcNow, count, seed, availableMinutes, RequireShortQuest: requireShort);

    public static QuestHistoryItem Completed(QuestCandidate c, int daysAgo, int? rating = null)
        => new(c.TemplateId, c.Category, c.InterestIds, QuestStatus.Completed,
            UtcNow.AddDays(-daysAgo - 1), UtcNow.AddDays(-daysAgo), null, rating, null);

    public static QuestHistoryItem Skipped(QuestCandidate c, SkipReason reason, int daysAgo)
        => new(c.TemplateId, c.Category, c.InterestIds, QuestStatus.Skipped,
            UtcNow.AddDays(-daysAgo), UtcNow.AddDays(-daysAgo), reason, null, null);

    public static RecommendationHistory History(IEnumerable<QuestHistoryItem> items, IEnumerable<Guid>? open = null)
    {
        var list = items.ToList();
        var completed = list.Where(i => i.Status == QuestStatus.Completed)
            .GroupBy(i => i.TemplateId)
            .ToDictionary(g => g.Key, g => g.Max(i => i.LastActivityAt));

        return new RecommendationHistory(list, open ?? [], completed,
            list.Where(i => i.Status == QuestStatus.Completed).Select(i => i.Category));
    }
}
