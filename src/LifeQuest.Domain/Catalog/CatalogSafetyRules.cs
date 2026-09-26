using LifeQuest.Domain.Common;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Domain.Catalog;

/// <summary>
/// Editoryal güvenlik kontrol listesi. Hem CI'da (katalog testleri) hem de çalışma zamanında (seed sırasında
/// kuralı çiğneyen template <see cref="SafetyLevel.NeedsReview"/> olur) uygulanır. Güvenlik tek boyutlu
/// değildir: risk skoru, fiziksel efor, açık hava ve saat birlikte değerlendirilir.
/// </summary>
public static class CatalogSafetyRules
{
    public const double MaxRiskScore = 0.3;
    public const int MinTitleLength = 5;
    public const int MaxTitleLength = 80;
    public const int MinDescriptionLength = 30;
    public const int MaxDescriptionLength = 300;

    public const int MinTemplatesPerCategory = 12;
    public const int MinDailyPerCategory = 2;
    public const double MinFreeShare = 0.35;
    public const double MinCityIndependentShare = 0.45;

    /// <summary>
    /// İlgi alanı derinliği: her ilgide en az bu kadar görev olmalı. Daha azında en sevilen görevler bekleme
    /// süresine girince öneri havuzu tükenir (offline simülasyon, bulgu 4).
    /// </summary>
    public const int MinTemplatesPerInterest = 4;

    /// <summary>Her ilgi alanında en az bir kısa görev: az vakti olan kullanıcı da o ilgiyi deneyebilmeli.</summary>
    public const int ShortQuestMaxMinutes = 60;

    /// <summary>Tek bir template'in editoryal kurallara uygunluğu.</summary>
    public static IReadOnlyList<string> ValidateTemplate(QuestTemplateSpec t)
    {
        var violations = new List<string>();

        if (t.IsOutdoor && t.DayParts.HasFlag(DayPart.Night))
            violations.Add(Text.Of("Açık hava quest'leri gece dilimine açık olamaz.", "Outdoor quests can't be open to the night slot."));

        if (t.RiskScore > MaxRiskScore)
            violations.Add($"Risk skoru {t.RiskScore} > {MaxRiskScore}; katalogda riskli aktivite bulunamaz.");

        if (t.Effort == PhysicalEffort.Vigorous && t.RiskScore <= 0)
            violations.Add(Text.Of("Yoğun eforlu quest'lerin risk skoru belirtilmelidir.", "Vigorous quests must have a risk score."));

        if (t.MinMinutes <= 0 || t.MinMinutes > t.MaxMinutes)
            violations.Add(Text.Of("Süre aralığı geçersiz.", "The duration range is invalid."));

        var durationRule = t.Type switch
        {
            QuestType.Daily when t.MaxMinutes > 30 => Text.Of("Günlük quest'ler en fazla 30 dakika sürmelidir.", "Daily quests must take at most 30 minutes."),
            QuestType.Weekly when t.MaxMinutes > 240 => Text.Of("Haftalık quest'ler en fazla 4 saat sürmelidir.", "Weekly quests must take at most 4 hours."),
            QuestType.Adventure when t.MinMinutes < 120 => Text.Of("Macera quest'leri en az 2 saat olmalıdır.", "Adventure quests must take at least 2 hours."),
            QuestType.Epic when t.MinMinutes < 300 => Text.Of("Destansı quest'ler en az 5 saat olmalıdır.", "Epic quests must take at least 5 hours."),
            _ => null
        };
        if (durationRule is not null)
            violations.Add(durationRule);

        if (t.InterestIds.Count == 0)
            violations.Add(Text.Of("En az bir ilgi alanı etiketi gerekir.", "At least one interest tag is required."));

        if (t.Title.Length is < MinTitleLength or > MaxTitleLength)
            violations.Add(Text.Of($"Başlık {MinTitleLength}-{MaxTitleLength} karakter olmalıdır.", $"The title must be {MinTitleLength}-{MaxTitleLength} characters."));

        if (t.Description.Length is < MinDescriptionLength or > MaxDescriptionLength)
            violations.Add(Text.Of($"Açıklama {MinDescriptionLength}-{MaxDescriptionLength} karakter olmalıdır.", $"The description must be {MinDescriptionLength}-{MaxDescriptionLength} characters."));

        if (t.SecondaryCategory == t.Category)
            violations.Add(Text.Of("İkincil kategori birincil kategoriyle aynı olamaz.", "The secondary category can't be the same as the primary."));

        return violations;
    }

    /// <summary>Kataloğun bütününe ait denge kuralları: çeşitlilik, erişilebilirlik (ücretsiz, şehirden bağımsız).</summary>
    public static IReadOnlyList<string> ValidateCatalog(IReadOnlyCollection<QuestTemplateSpec> templates)
    {
        var violations = new List<string>();
        if (templates.Count == 0)
            return [Text.Of("Katalog boş.", "The catalog is empty.")];

        foreach (var duplicate in templates.GroupBy(t => t.Code).Where(g => g.Count() > 1))
            violations.Add($"Kod tekrar ediyor: {duplicate.Key}");

        foreach (var category in LifeCategories.All)
        {
            var inCategory = templates.Where(t => t.Category == category).ToList();
            if (inCategory.Count < MinTemplatesPerCategory)
                violations.Add($"{category}: {inCategory.Count} template (en az {MinTemplatesPerCategory}).");
            if (inCategory.Count(t => t.Type == QuestType.Daily) < MinDailyPerCategory)
                violations.Add(Text.Of($"{category}: en az {MinDailyPerCategory} günlük quest gerekir.", $"{category}: at least {MinDailyPerCategory} daily quests are required."));
        }

        var freeShare = templates.Count(t => t.Cost == CostBand.Free) / (double)templates.Count;
        if (freeShare < MinFreeShare)
            violations.Add(Text.Of($"Ücretsiz quest payı %{freeShare * 100:0} (en az %{MinFreeShare * 100:0}).", $"Free quest share is {freeShare * 100:0}% (at least {MinFreeShare * 100:0}%)."));

        var cityIndependentShare = templates.Count(t => !t.RequiresCity) / (double)templates.Count;
        if (cityIndependentShare < MinCityIndependentShare)
            violations.Add(Text.Of($"Şehirden bağımsız quest payı %{cityIndependentShare * 100:0} (en az %{MinCityIndependentShare * 100:0}).", $"City-independent quest share is {cityIndependentShare * 100:0}% (at least {MinCityIndependentShare * 100:0}%)."));

        return violations;
    }

    /// <summary>İlgi alanı kapsaması: her ilgide yeterli sayıda ve en az bir kısa görev var mı?</summary>
    /// <param name="interests">İlgi kimliği → görünen ad (uyarı metni için).</param>
    public static IReadOnlyList<string> ValidateInterestCoverage(
        IReadOnlyCollection<QuestTemplateSpec> templates, IReadOnlyDictionary<Guid, string> interests)
    {
        var violations = new List<string>();
        foreach (var (id, name) in interests.OrderBy(i => i.Value, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)))
        {
            var tagged = templates.Where(t => t.InterestIds.Contains(id)).ToList();
            if (tagged.Count < MinTemplatesPerInterest)
                violations.Add(Text.Of($"{name}: {tagged.Count} görev (en az {MinTemplatesPerInterest}).", $"{name}: {tagged.Count} quests (at least {MinTemplatesPerInterest})."));
            if (tagged.Count > 0 && !tagged.Any(t => t.MaxMinutes <= ShortQuestMaxMinutes))
                violations.Add(Text.Of($"{name}: {ShortQuestMaxMinutes} dakikalık kısa görev yok.", $"{name}: no short quest of {ShortQuestMaxMinutes} minutes."));
        }

        return violations;
    }
}
