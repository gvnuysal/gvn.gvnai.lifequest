using System.Globalization;
using System.Text.RegularExpressions;

namespace LifeQuest.Application.Safety;

/// <summary>
/// Serbest metin için ortak içerik taraması: AI anlatımı (<see cref="Narration.NarrationGuard"/>) ve topluluk
/// fikirleri aynı kuralları kullanır. Sonuç yalnızca bulguları döner; neyin reddedileceğine çağıran karar verir.
/// </summary>
public static partial class ContentScreen
{
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
            "tren yolu", "raylar", "otostop", "izinsiz", "gizlice", "yasak bölge", "hız yap", "hız yarış", "tehlikeli",
            // İngilizce içerik (fikirler ve şablonlar iki dilde yazılabilir)
            "alcohol", "beer", "wine", "liquor", "vodka", "whisk", "drunk", "gambl", "betting", "casino",
            "deserted", "midnight", "alone at night", "in the dark", "cliff", "rooftop", "on the roof",
            "railway", "train track", "hitchhik", "trespass", "secretly", "restricted area", "forbidden area",
            "speeding", "street rac", "dangerous"
        }.Select(p => new Regex(@"(?<!\p{L})" + p, RegexOptions.Compiled | RegexOptions.CultureInvariant))
    ];

    private static readonly string[] PersonalDataRequests =
    [
        "konumunu paylaş", "konum paylaş", "konumunu gönder", "canlı konum", "fotoğrafını paylaş",
        "fotoğraf paylaş", "fotoğrafını gönder", "adresini", "telefon numaran", "kimlik", "şifre",
        "share your location", "live location", "send your location", "share your photo", "send your photo",
        "send a photo", "your address", "phone number", "id card", "password"
    ];

    public static string Normalize(string text) => " " + text.ToLower(Turkish) + " ";

    /// <summary>İngilizce metin için: Türkçe küçültme "I"yı "ı" yapar; iki biçim de denenir.</summary>
    private static IEnumerable<string> Forms(string text) => [Normalize(text), " " + text.ToLowerInvariant() + " "];

    public static bool ContainsUrl(string text) => UrlPattern().IsMatch(text);

    public static bool ContainsMarkup(string text) => text.Contains('<') || text.Contains('>');

    /// <summary>E-posta adresi veya telefon numarası (paylaşılan iletişim bilgisi: spam ve mahremiyet riski).</summary>
    public static bool ContainsContactInfo(string text) => EmailPattern().IsMatch(text) || PhonePattern().IsMatch(text);

    public static bool ContainsRiskyContent(string text)
    {
        return Forms(text).Any(lower => RiskyPatterns.Any(p => p.IsMatch(lower)));
    }

    public static bool RequestsPersonalData(string text)
    {
        return Forms(text).Any(lower => PersonalDataRequests.Any(lower.Contains));
    }

    [GeneratedRegex(@"(https?://|www\.|\b[a-z0-9-]+\.(com|net|org|io|app|com\.tr|tr)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.]+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    /// <summary>En az 10 rakam içeren telefon benzeri dizi (boşluk, tire, parantez ve + serbest).</summary>
    [GeneratedRegex(@"(\+?\d[\s\-()]*){10,}")]
    private static partial Regex PhonePattern();
}
