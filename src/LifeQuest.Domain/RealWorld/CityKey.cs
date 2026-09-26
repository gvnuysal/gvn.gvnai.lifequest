using System.Text;

namespace LifeQuest.Domain.RealWorld;

/// <summary>
/// Serbest metin şehir adını eşleştirme anahtarına çevirir: "İstanbul", "istanbul" ve "Istanbul" aynı anahtarı verir.
/// Türkçe harfler ASCII karşılığına indirilir, boşluklar sadeleştirilir.
/// </summary>
public static class CityKey
{
    public const int MaxLength = 80;

    public static string Normalize(string city)
    {
        var builder = new StringBuilder(city.Length);
        var lastWasSpace = true;
        foreach (var ch in city.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace) builder.Append(' ');
                lastWasSpace = true;
                continue;
            }

            lastWasSpace = false;
            builder.Append(ch switch
            {
                'ı' or 'I' or 'İ' or 'i' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                _ => char.ToLowerInvariant(ch)
            });
        }

        return builder.ToString().TrimEnd();
    }
}
