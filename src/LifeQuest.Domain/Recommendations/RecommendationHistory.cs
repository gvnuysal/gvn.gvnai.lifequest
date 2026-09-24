using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Domain.Recommendations;

/// <summary>Kullanıcının geçmiş sinyallerinin engine için hazırlanmış görünümü.</summary>
public sealed class RecommendationHistory
{
    public static readonly RecommendationHistory Empty = new([], [], new Dictionary<Guid, DateTime>(), []);

    public IReadOnlyList<QuestHistoryItem> Items { get; }
    public IReadOnlySet<Guid> ActiveTemplateIds { get; }

    /// <summary>Tüm zamanlarda tamamlanan template'ler ve son tamamlanma zamanı.</summary>
    public IReadOnlyDictionary<Guid, DateTime> CompletedTemplates { get; }

    /// <summary>Tüm zamanlarda en az bir kez tamamlanmış kategoriler.</summary>
    public IReadOnlySet<LifeCategory> CompletedCategories { get; }

    public RecommendationHistory(
        IEnumerable<QuestHistoryItem> recentItems,
        IEnumerable<Guid> activeTemplateIds,
        IReadOnlyDictionary<Guid, DateTime> completedTemplates,
        IEnumerable<LifeCategory> completedCategories)
    {
        Items = recentItems.ToList();
        ActiveTemplateIds = activeTemplateIds.ToHashSet();
        CompletedTemplates = completedTemplates;
        CompletedCategories = completedCategories.ToHashSet();
    }

    public IEnumerable<QuestHistoryItem> Since(DateTime utc) => Items.Where(i => i.LastActivityAt >= utc);
}
