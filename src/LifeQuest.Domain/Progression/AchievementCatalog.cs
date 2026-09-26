using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Progression;

public sealed record AchievementDefinition(
    string Code,
    Localization.LocalizedText Title,
    Localization.LocalizedText Description,
    Func<PlayerProgress, bool> IsSatisfied);

/// <summary>
/// Başarımlar deterministik kurallardır ve kodda tanımlıdır (test edilebilir, versiyonlanabilir).
/// Bilinçli olarak kayıp korkusu yaratan streak / "günlük giriş" başarımları yoktur.
/// </summary>
public static class AchievementCatalog
{
    public static readonly IReadOnlyList<AchievementDefinition> All = Build();

    public static AchievementDefinition? Find(string code) => All.FirstOrDefault(a => a.Code == code);

    private static List<AchievementDefinition> Build()
    {
        var list = new List<AchievementDefinition>
        {
            new("FIRST_STEP", new("İlk Adım", "First Step"), new("İlk quest'ini tamamladın.", "You completed your first quest."),
                p => p.TotalCompleted >= 1),
            new("TEN_QUESTS", new("Yola Çıktın", "On Your Way"), new("10 quest tamamladın.", "You completed 10 quests."),
                p => p.TotalCompleted >= 10),
            new("FIFTY_QUESTS", new("Deneyim Avcısı", "Experience Hunter"), new("50 quest tamamladın.", "You completed 50 quests."),
                p => p.TotalCompleted >= 50),
            new("ALL_ROUNDER", new("Çok Yönlü", "All-Rounder"),
                new("Her Life Profile alanında en az bir quest tamamladın.", "You completed at least one quest in every Life Profile area."),
                p => p.Categories.All(c => c.CompletedCount > 0)),
            new("REFLECTIVE", new("Kendini Tanıyan", "Self-Aware"),
                new("5 quest'i değerlendirdin; önerilerin artık daha isabetli.", "You rated 5 quests; your suggestions are now more accurate."),
                p => p.FeedbackCount >= 5)
        };

        list.AddRange(LifeCategories.All.Select(category => new AchievementDefinition(
            $"{category.ToString().ToUpperInvariant()}_5",
            new($"{category.LocalizedName().Tr} Meraklısı", $"{category.LocalizedName().En} Enthusiast"),
            new($"{category.LocalizedName().Tr} alanında 5 quest tamamladın.", $"You completed 5 {category.LocalizedName().En} quests."),
            p => p.CategoryOf(category).CompletedCount >= 5)));

        return list;
    }
}
