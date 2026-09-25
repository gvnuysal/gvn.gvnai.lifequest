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
            "tren yolu", "raylar", "otostop", "izinsiz", "gizlice", "yasak bölge", "hız yap", "hız yarış", "tehlikeli"
        }.Select(p => new Regex(@"(?<!\p{L})" + p, RegexOptions.Compiled | RegexOptions.CultureInvariant))
    ];

    private static readonly string[] PersonalDataRequests =
    [
        "konumunu paylaş", "konum paylaş", "konumunu gönder", "canlı konum", "fotoğrafını paylaş",
        "fotoğraf paylaş", "fotoğrafını gönder", "adresini", "telefon numaran", "kimlik", "şifre"
    ];

    public static string Normalize(string text) => " " + text.ToLower(Turkish) + " ";

    public static bool ContainsUrl(string text) => UrlPattern().IsMatch(text);

    public static bool ContainsMarkup(string text) => text.Contains('<') || text.Contains('>');

    /// <summary>E-posta adresi veya telefon numarası (paylaşılan iletişim bilgisi: spam ve mahremiyet riski).</summary>
    public static bool ContainsContactInfo(string text) => EmailPattern().IsMatch(text) || PhonePattern().IsMatch(text);

    public static bool ContainsRiskyContent(string text)
    {
        var lower = Normalize(text);
        return RiskyPatterns.Any(p => p.IsMatch(lower));
    }

    public static bool RequestsPersonalData(string text)
    {
        var lower = Normalize(text);
        return PersonalDataRequests.Any(lower.Contains);
    }

    [GeneratedRegex(@"(https?://|www\.|\b[a-z0-9-]+\.(com|net|org|io|app|com\.tr|tr)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.]+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    /// <summary>En az 10 rakam içeren telefon benzeri dizi (boşluk, tire, parantez ve + serbest).</summary>
    [GeneratedRegex(@"(\+?\d[\s\-()]*){10,}")]
    private static partial Regex PhonePattern();
}
