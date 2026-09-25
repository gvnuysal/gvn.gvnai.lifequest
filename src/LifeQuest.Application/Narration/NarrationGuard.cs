using System.Text.RegularExpressions;
using LifeQuest.Application.Safety;

namespace LifeQuest.Application.Narration;

/// <summary>
/// LLM çıktısı kullanıcıya gitmeden önceki guardrail: şema/uzunluk, bağlantı ve para ifadesi yasağı,
/// template'te olmayan sayıların (uydurulmuş süre/fiyat) reddi, riskli içerik ve kişisel veri isteği filtresi.
/// Herhangi bir ihlal template metnine geri dönüşe yol açar.
/// </summary>
public static partial class NarrationGuard
{
    public const int MaxTitleLength = 80;
    public const int MaxDescriptionLength = 300;
    public const int MinDescriptionLength = 20;

    private static readonly string[] MoneyAndRewardTerms = ["₺", " tl", "lira", "$", "€", "xp", "puan kazan"];

    public static IReadOnlyList<string> Validate(QuestNarration output, NarrationRequest request)
    {
        var violations = new List<string>();
        var title = output.Title?.Trim() ?? string.Empty;
        var description = output.Description?.Trim() ?? string.Empty;

        if (title.Length == 0 || title.Length > MaxTitleLength)
            violations.Add("title_length");

        if (description.Length < MinDescriptionLength || description.Length > MaxDescriptionLength)
            violations.Add("description_length");

        var text = $"{title} {description}";
        var lower = ContentScreen.Normalize(text);

        if (ContentScreen.ContainsUrl(text))
            violations.Add("url");

        if (ContentScreen.ContainsMarkup(text))
            violations.Add("markup");

        if (MoneyAndRewardTerms.Any(lower.Contains))
            violations.Add("money_or_reward");

        var allowedNumbers = NumberPattern().Matches($"{request.BaseTitle} {request.BaseDescription}")
            .Select(m => m.Value).ToHashSet();
        if (NumberPattern().Matches(text).Any(m => !allowedNumbers.Contains(m.Value)))
            violations.Add("invented_number");

        if (ContentScreen.ContainsRiskyContent(text))
            violations.Add("risky_content");

        if (ContentScreen.RequestsPersonalData(text))
            violations.Add("personal_data_request");

        return violations;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberPattern();
}
