using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Gvn.GvnFramework.Caching.Abstractions;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.RealWorld;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeQuest.Infrastructure.RealWorld;

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>Kapalıysa öneriler havadan etkilenmez (ör. dış ağa çıkışı olmayan ortam).</summary>
    public bool Enabled { get; set; } = true;

    public string GeocodingUrl { get; set; } = "https://geocoding-api.open-meteo.com/v1/search";
    public string ForecastUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

    /// <summary>Aynı adlı şehirlerde tercih edilecek ülke (ISO-3166 alfa-2). Boşsa en iyi eşleşme.</summary>
    public string? CountryCode { get; set; } = "TR";

    public int ForecastCacheMinutes { get; set; } = 30;
}

/// <summary>
/// Open-Meteo (anahtarsız, ücretsiz) ile şehir → koordinat → saatlik tahmin. Koordinatlar 30 gün, tahmin 30 dakika
/// önbelleklenir; aynı şehirdeki tüm kullanıcılar tek istek paylaşır. Servise yalnızca şehir adı gider.
/// </summary>
internal sealed class OpenMeteoWeatherProvider(
    HttpClient http,
    ICacheService cache,
    IOptions<WeatherOptions> options,
    TimeProvider clock,
    ILogger<OpenMeteoWeatherProvider> logger) : IWeatherProvider
{
    private static readonly TimeSpan GeocodeCache = TimeSpan.FromDays(30);
    private static readonly TimeSpan MissCache = TimeSpan.FromMinutes(5);

    public async Task<WeatherSnapshot?> GetAsync(string city, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || string.IsNullOrWhiteSpace(city))
            return null;

        var geoKey = $"lifequest:geo:v1:{CityKey.Normalize(city)}";
        var place = await cache.GetAsync<CachedPlace>(geoKey, cancellationToken);
        if (place is null)
        {
            place = new CachedPlace(await GeocodeAsync(city, cancellationToken));
            // Bulunamayan/hata veren şehir kısa süre sonra tekrar denenir; bulunan koordinat uzun süre saklanır.
            await cache.SetAsync(geoKey, place, place.Place is null ? MissCache : GeocodeCache, cancellationToken);
        }
        if (place.Place is not { } point)
            return null;

        var forecastKey = string.Create(CultureInfo.InvariantCulture, $"lifequest:weather:v1:{point.Latitude:F2}:{point.Longitude:F2}");
        var forecast = await cache.GetAsync<CachedForecast>(forecastKey, cancellationToken);
        if (forecast is null)
        {
            forecast = new CachedForecast(await ForecastAsync(point, cancellationToken));
            await cache.SetAsync(forecastKey, forecast,
                forecast.Snapshot is null ? MissCache : TimeSpan.FromMinutes(options.Value.ForecastCacheMinutes), cancellationToken);
        }

        return forecast.Snapshot;
    }

    private async Task<GeoPoint?> GeocodeAsync(string city, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{options.Value.GeocodingUrl}?name={Uri.EscapeDataString(city.Trim())}&count=5&language=tr&format=json";
            var response = await http.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);
            var results = response?.Results ?? [];
            var country = options.Value.CountryCode;
            var best = results.FirstOrDefault(r => !string.IsNullOrEmpty(country) &&
                                                   string.Equals(r.CountryCode, country, StringComparison.OrdinalIgnoreCase))
                       ?? results.FirstOrDefault();
            return best is null ? null : new GeoPoint(best.Name, best.Latitude, best.Longitude);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
                                   && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Geocoding failed");
            return null;
        }
    }

    private async Task<WeatherSnapshot?> ForecastAsync(GeoPoint point, CancellationToken cancellationToken)
    {
        try
        {
            var url = FormattableString.Invariant($"{options.Value.ForecastUrl}?latitude={point.Latitude:F4}&longitude={point.Longitude:F4}") +
                "&current=temperature_2m,weather_code,wind_speed_10m,is_day" +
                "&hourly=temperature_2m,precipitation_probability,weather_code,wind_speed_10m" +
                "&forecast_days=2&timezone=auto";
            var response = await http.GetFromJsonAsync<ForecastResponse>(url, cancellationToken);
            if (response?.Current is not { } current || response.Hourly is not { } hourly)
                return null;

            var hours = hourly.Time
                .Select((time, i) => new HourlyWeather(
                    DateTime.Parse(time, CultureInfo.InvariantCulture),
                    At(hourly.Temperature, i), (int)At(hourly.PrecipitationProbability, i),
                    (int)At(hourly.WeatherCode, i), At(hourly.WindSpeed, i)))
                .ToList();

            return new WeatherSnapshot(point.Name, clock.GetUtcNow().UtcDateTime, current.Temperature ?? 0,
                (int)(current.WeatherCode ?? 0), current.WindSpeed ?? 0, current.IsDay != 0, hours);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or FormatException
                                   && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Weather forecast failed for {City}", point.Name);
            return null;
        }

        static double At(IReadOnlyList<double?>? values, int i) => values is not null && i < values.Count ? values[i] ?? 0 : 0;
    }

    public sealed record GeoPoint(string Name, double Latitude, double Longitude);

    public sealed record CachedPlace(GeoPoint? Place);

    public sealed record CachedForecast(WeatherSnapshot? Snapshot);

    private sealed record GeocodingResponse([property: JsonPropertyName("results")] List<GeocodingResult>? Results);

    private sealed record GeocodingResult(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("country_code")] string? CountryCode);

    private sealed record ForecastResponse(
        [property: JsonPropertyName("current")] CurrentBlock? Current,
        [property: JsonPropertyName("hourly")] HourlyBlock? Hourly);

    private sealed record CurrentBlock(
        [property: JsonPropertyName("temperature_2m")] double? Temperature,
        [property: JsonPropertyName("weather_code")] double? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] double? WindSpeed,
        [property: JsonPropertyName("is_day")] int IsDay);

    private sealed record HourlyBlock(
        [property: JsonPropertyName("time")] List<string> Time,
        [property: JsonPropertyName("temperature_2m")] List<double?>? Temperature,
        [property: JsonPropertyName("precipitation_probability")] List<double?>? PrecipitationProbability,
        [property: JsonPropertyName("weather_code")] List<double?>? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] List<double?>? WindSpeed);
}
