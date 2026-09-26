using System.Globalization;

namespace LifeQuest.Domain.Localization;

/// <summary>
/// Desteklenen diller: Türkçe (varsayılan) ve İngilizce. Geçerli dil <see cref="CultureInfo.CurrentUICulture"/>'dan okunur:
/// API'de istek kültürü (Accept-Language), arka plan işlerinde <see cref="Use"/> ile kullanıcının hesap dili.
/// </summary>
public static class Language
{
    public const string Turkish = "tr";
    public const string English = "en";
    public const string Default = Turkish;

    public static readonly IReadOnlyList<string> Supported = [Turkish, English];

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>O anki dil: "en" ile başlayan kültürlerde İngilizce, diğer her durumda Türkçe.</summary>
    public static string Current => IsEnglish(CultureInfo.CurrentUICulture) ? English : Turkish;

    public static bool IsCurrentEnglish => Current == English;

    /// <summary>O anki dilin kültürü (büyük/küçük harf, tarih ve sayı biçimi için).</summary>
    public static CultureInfo CurrentCulture => CultureOf(Current);

    public static CultureInfo CultureOf(string language) => Normalize(language) == English ? EnglishCulture : TurkishCulture;

    public static bool IsSupported(string? language)
        => language is not null && Supported.Contains(language.Trim().ToLowerInvariant());

    /// <summary>"en-GB", "EN" → "en"; tanınmayan ya da boş → varsayılan (tr).</summary>
    public static string Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return Default;
        var code = language.Trim().ToLowerInvariant();
        return code.StartsWith(English, StringComparison.Ordinal) ? English
             : code.StartsWith(Turkish, StringComparison.Ordinal) ? Turkish
             : Default;
    }

    /// <summary>
    /// Arka plan işi gibi istek dışı kodda, kullanıcı başına metinlerin o dilde üretilmesi için kültürü geçici olarak
    /// değiştirir; dispose edilince önceki kültür geri gelir.
    /// </summary>
    public static IDisposable Use(string? language)
    {
        var previous = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var culture = CultureOf(Normalize(language));
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        return new Restore(previous.CurrentCulture, previous.CurrentUICulture);
    }

    private static bool IsEnglish(CultureInfo culture)
        => culture.TwoLetterISOLanguageName.Equals(English, StringComparison.OrdinalIgnoreCase);

    private sealed class Restore(CultureInfo culture, CultureInfo uiCulture) : IDisposable
    {
        public void Dispose()
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }
}

/// <summary>
/// İki dilli metin seçici: kaynakta Türkçe ve İngilizce aynı satırda durur, eksik çeviri derlenmez.
/// <code>Error.Conflict("QUEST_EXPIRED", Text.Of("Bu quest'in süresi doldu.", "This quest has expired."))</code>
/// </summary>
public static class Text
{
    /// <summary>İngilizce belirsiz tanımlık: "an Exploration quest", "a Culture quest".</summary>
    public static string Article(string word)
        => word.Length > 0 && "AEIOUaeiou".Contains(word[0]) ? "an" : "a";

    public static string Of(string tr, string en) => Language.IsCurrentEnglish ? en : tr;

    /// <summary>O anki dilin kültürüyle biçimlenen metin (tarih/sayı içeren mesajlar).</summary>
    public static string Format(FormattableString tr, FormattableString en)
        => Language.IsCurrentEnglish ? en.ToString(Language.CurrentCulture) : tr.ToString(Language.CurrentCulture);
}

/// <summary>Aynı anda iki dilde üretilip saklanan metin (görev kopyaları, öneri gerekçeleri).</summary>
public sealed record LocalizedText(string Tr, string En)
{
    public string Current => Language.IsCurrentEnglish ? En : Tr;

    public string In(string language) => Language.Normalize(language) == Language.English ? En : Tr;

    /// <summary>İngilizcesi henüz girilmemiş içerik (ör. eski kayıt) Türkçeye düşer.</summary>
    public static LocalizedText WithFallback(string tr, string? en) => new(tr, string.IsNullOrWhiteSpace(en) ? tr : en);

    public static LocalizedText Same(string text) => new(text, text);

    public override string ToString() => Current;
}
