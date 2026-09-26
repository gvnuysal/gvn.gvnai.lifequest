namespace LifeQuest.Domain.RealWorld;

/// <summary>Açık hava görevleri için havanın uygunluğu. Bilinmiyorsa öneriler hava durumundan etkilenmez.</summary>
public enum OutdoorWeather
{
    Unknown = 0,
    Good = 1,
    Poor = 2
}

/// <param name="Time">Yerel saat (şehrin saat dilimi).</param>
/// <param name="PrecipitationProbability">Yüzde (0–100).</param>
/// <param name="WeatherCode">WMO hava kodu.</param>
public sealed record HourlyWeather(DateTime Time, double TemperatureC, int PrecipitationProbability, int WeatherCode, double WindKmh);

/// <summary>Bir şehir için anlık durum ve önümüzdeki saatler (Open-Meteo'dan, sunucuda önbelleklenir).</summary>
public sealed record WeatherSnapshot(
    string City,
    DateTime FetchedAtUtc,
    double TemperatureC,
    int WeatherCode,
    double WindKmh,
    bool IsDay,
    IReadOnlyList<HourlyWeather> Hours);

public sealed record WeatherVerdict(OutdoorWeather Outdoor, string? Reason);

/// <summary>
/// Açık hava için eşikler: yağış olasılığı ≥ %60 ya da yağışlı/fırtınalı kod, 3 °C altı veya 35 °C üstü, 45 km/s üstü rüzgâr
/// "uygun değil" sayılır. Pencere, önerinin yapılacağı zaman aralığıdır (günlük öneride gündüz saatleri, bağlamsal
/// öneride önümüzdeki birkaç saat).
/// </summary>
public static class WeatherAssessment
{
    public const int RainProbabilityThreshold = 60;
    public const double ColdThresholdC = 3;
    public const double HotThresholdC = 35;
    public const double WindThresholdKmh = 45;

    public static WeatherVerdict Assess(WeatherSnapshot snapshot, DateTime fromLocal, DateTime toLocal)
    {
        var hours = snapshot.Hours.Where(h => h.Time >= fromLocal.AddMinutes(-59) && h.Time <= toLocal).ToList();
        if (hours.Count == 0)
            return new WeatherVerdict(OutdoorWeather.Unknown, null);

        if (hours.Any(h => IsStorm(h.WeatherCode)))
            return new WeatherVerdict(OutdoorWeather.Poor, "fırtına bekleniyor");
        if (hours.Any(h => h.PrecipitationProbability >= RainProbabilityThreshold || IsWet(h.WeatherCode)))
            return new WeatherVerdict(OutdoorWeather.Poor, hours.Any(h => IsSnow(h.WeatherCode)) ? "kar bekleniyor" : "yağmur bekleniyor");
        if (hours.Max(h => h.WindKmh) > WindThresholdKmh)
            return new WeatherVerdict(OutdoorWeather.Poor, "kuvvetli rüzgâr bekleniyor");

        var max = hours.Max(h => h.TemperatureC);
        var min = hours.Min(h => h.TemperatureC);
        if (max > HotThresholdC)
            return new WeatherVerdict(OutdoorWeather.Poor, "aşırı sıcak bekleniyor");
        if (min < ColdThresholdC)
            return new WeatherVerdict(OutdoorWeather.Poor, "hava çok soğuk");

        return new WeatherVerdict(OutdoorWeather.Good, null);
    }

    /// <summary>Günlük öneri penceresi: gündüz (09–21); bu saatler geçtiyse önümüzdeki 3 saat.</summary>
    public static (DateTime From, DateTime To) DailyWindow(DateTime localNow)
    {
        var from = localNow.Date.AddHours(9) > localNow ? localNow.Date.AddHours(9) : localNow;
        var to = localNow.Date.AddHours(21);
        return from < to ? (from, to) : (localNow, localNow.AddHours(3));
    }

    public static string Describe(int code) => code switch
    {
        0 => "Açık",
        1 or 2 => "Parçalı bulutlu",
        3 => "Kapalı",
        45 or 48 => "Sisli",
        51 or 53 or 55 or 56 or 57 => "Çiseleme",
        61 or 63 or 66 => "Yağmurlu",
        65 or 67 => "Kuvvetli yağmur",
        71 or 73 or 75 or 77 => "Karlı",
        80 or 81 or 82 => "Sağanak",
        85 or 86 => "Kar sağanağı",
        95 or 96 or 99 => "Gök gürültülü fırtına",
        _ => "Değişken"
    };

    private static bool IsStorm(int code) => code is 95 or 96 or 99;

    private static bool IsSnow(int code) => code is 71 or 73 or 75 or 77 or 85 or 86;

    // Hafif çiseleme (51) tek başına açık havayı engellemez.
    private static bool IsWet(int code) => code is 53 or 55 or 56 or 57 or 61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 || IsSnow(code);
}
