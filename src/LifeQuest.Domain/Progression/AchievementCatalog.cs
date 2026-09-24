using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Progression;

public sealed record AchievementDefinition(
    string Code,
    string Title,
    string Description,
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
            new("FIRST_STEP", "İlk Adım", "İlk quest'ini tamamladın.", p => p.TotalCompleted >= 1),
            new("TEN_QUESTS", "Yola Çıktın", "10 quest tamamladın.", p => p.TotalCompleted >= 10),
            new("FIFTY_QUESTS", "Deneyim Avcısı", "50 quest tamamladın.", p => p.TotalCompleted >= 50),
            new("ALL_ROUNDER", "Çok Yönlü", "Her Life Profile alanında en az bir quest tamamladın.",
                p => p.Categories.All(c => c.CompletedCount > 0)),
            new("REFLECTIVE", "Kendini Tanıyan", "5 quest'i değerlendirdin; önerilerin artık daha isabetli.",
                p => p.FeedbackCount >= 5)
        };

        list.AddRange(LifeCategories.All.Select(category => new AchievementDefinition(
            $"{category.ToString().ToUpperInvariant()}_5",
            $"{category.DisplayName()} Meraklısı",
            $"{category.DisplayName()} alanında 5 quest tamamladın.",
            p => p.CategoryOf(category).CompletedCount >= 5)));

        return list;
    }
}
