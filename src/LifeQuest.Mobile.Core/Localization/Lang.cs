using System.ComponentModel;
using System.Globalization;

namespace LifeQuest.Mobile.Core.Localization;

public enum AppLanguage
{
    Tr,
    En
}

/// <summary>
/// Uygulamanın o anki dili. Cihaz dili Türkçe ise Türkçe, diğer her durumda İngilizce (web ile aynı kural);
/// kullanıcı seçimi saklanır, girişten sonra profildeki dil geçerli olur.
/// </summary>
public static class Lang
{
    private static AppLanguage _current = Detect(null, CultureInfo.CurrentUICulture.Name);

    public static AppLanguage Current => _current;

    /// <summary>API'ye giden ve profilde saklanan kod: "tr" / "en".</summary>
    public static string Code => ToCode(_current);

    /// <summary>Tarih ve sayı biçimleri için kültür (web'deki tr-TR / en-GB).</summary>
    public static CultureInfo Culture => CultureOf(_current);

    public static event EventHandler? Changed;

    public static AppLanguage Detect(string? stored, string? deviceCulture)
    {
        if (TryParse(stored, out var language))
            return language;
        return (deviceCulture ?? "").StartsWith("tr", StringComparison.OrdinalIgnoreCase) ? AppLanguage.Tr : AppLanguage.En;
    }

    public static bool TryParse(string? code, out AppLanguage language)
    {
        switch (code?.Trim().ToLowerInvariant())
        {
            case "tr":
                language = AppLanguage.Tr;
                return true;
            case "en":
                language = AppLanguage.En;
                return true;
            default:
                language = AppLanguage.Tr;
                return false;
        }
    }

    public static string ToCode(AppLanguage language) => language == AppLanguage.En ? "en" : "tr";

    public static CultureInfo CultureOf(AppLanguage language)
        => CultureInfo.GetCultureInfo(language == AppLanguage.En ? "en-GB" : "tr-TR");

    public static void Set(AppLanguage language)
    {
        if (_current == language)
            return;
        _current = language;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}

/// <summary>Sözlük sınıflarının tabanı: her okuma o anki dilde metin döndürür.</summary>
public abstract class LocalizedStrings
{
    protected static T T<T>(T tr, T en) => Lang.Current == AppLanguage.En ? en : tr;
}

/// <summary>
/// XAML bağlamaları için dil kaynağı. Dil değişince <see cref="S"/> yeni bir örnekle değişir; bağlamalar
/// (<c>{l:Tr Today.Title}</c>) yeniden okunur ve ekran sayfa yenilenmeden yeni dilde görünür.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    public static Localizer Instance { get; } = new();

    private Localizer() => Lang.Changed += (_, _) =>
    {
        S = new Strings();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(S)));
    };

    public Strings S { get; private set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}
