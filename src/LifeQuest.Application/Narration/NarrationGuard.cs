using System.Globalization;
using System.Text.RegularExpressions;

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

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Riskli ifadeler kelime başından eşleşir (Türkçe ekler serbest), ancak masum kelimelerle çakışmalar
    /// hariç tutulur: "biraz" ≠ "bira", "rakım" ≠ "rakı". "Yarış" gibi bağlama göre masum olan kelimeler
    /// ("bilgi yarışması") yalnızca riskli bağlamıyla ("hız yarışı") listelenir.
    /// </summary>
    private static readonly Regex[] RiskyPatterns =
    [
        .. new[]
        {
            "alkol", "içki", "bira(?!z|der)", "şarap", "rakı(?!m)", "sarhoş", "kumar", "bahis",
            "ıssız", "gece yarısı", "tek başına gece", "karanlıkta", "yüksekten", "uçurum", "çatıya", "çatıda",
            "tren yolu", "raylar", "otostop", "izinsiz", "gizlice", "yasak bölge", "hız yap", "hız yarış", "tehlikeli"
        }.Select(p => new Regex(@"(?<!\p{L})" + p, RegexOptions.Compiled | RegexOptions.CultureInvariant))
    ];

    private static readonly string[] PersonalDataRequests =
    [
        "konumunu paylaş", "konum paylaş", "konumunu gönder", "canlı konum", "fotoğrafını paylaş",
        "fotoğraf paylaş", "fotoğrafını gönder", "adresini", "telefon numaran", "kimlik", "şifre"
    ];

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
        var lower = " " + text.ToLower(Turkish) + " ";

        if (UrlPattern().IsMatch(text))
            violations.Add("url");

        if (text.Contains('<') || text.Contains('>'))
            violations.Add("markup");

        if (MoneyAndRewardTerms.Any(lower.Contains))
            violations.Add("money_or_reward");

        var allowedNumbers = NumberPattern().Matches($"{request.BaseTitle} {request.BaseDescription}")
            .Select(m => m.Value).ToHashSet();
        if (NumberPattern().Matches(text).Any(m => !allowedNumbers.Contains(m.Value)))
            violations.Add("invented_number");

        if (RiskyPatterns.Any(p => p.IsMatch(lower)))
            violations.Add("risky_content");

        if (PersonalDataRequests.Any(p => lower.Contains(p)))
            violations.Add("personal_data_request");

        return violations;
    }

    [GeneratedRegex(@"(https?://|www\.)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberPattern();
}
