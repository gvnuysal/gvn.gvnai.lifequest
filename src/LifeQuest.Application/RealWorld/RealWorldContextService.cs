using LifeQuest.Domain.Localization;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.RealWorld;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Application.RealWorld;

/// <param name="Outdoor">"Uygun değil" ise açık hava görevine uyarı verilir.</param>
/// <param name="Code">WMO hava kodu (istemci ikonu buna göre seçer, metne göre değil).</param>
public sealed record WeatherDto(string City, int TemperatureC, string Summary, OutdoorWeather Outdoor, string? Advice, int Code = 0);

public sealed record RealWorldContext(OutdoorWeather Weather, IReadOnlySet<Guid> EventTemplateIds, WeatherDto? WeatherDto)
{
    public static readonly RealWorldContext None = new(OutdoorWeather.Unknown, new HashSet<Guid>(), null);
}

/// <summary>
/// Öneri bağlamına gerçek dünyayı ekler: kullanıcının şehrindeki hava ve yaklaşan etkinlikler. Şehri olmayan
/// kullanıcıda ya da dış servis hata verdiğinde bağlam boş kalır; öneri akışı hiçbir zaman buna takılmaz.
/// </summary>
public sealed class RealWorldContextService(
    IWeatherProvider weather,
    ILocalPlaceRepository places,
    ILogger<RealWorldContextService> logger)
{
    /// <summary>Etkinlik bonusu için ileriye bakış: bu hafta içindeki etkinlikler.</summary>
    public static readonly TimeSpan EventHorizon = TimeSpan.FromDays(7);

    /// <param name="fromLocal">Önerinin yapılacağı pencerenin başı (kullanıcının yerel saati).</param>
    public async Task<RealWorldContext> ForAsync(
        UserProfile profile, DateTime nowUtc, DateTime fromLocal, DateTime toLocal, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profile.City))
            return RealWorldContext.None;

        var cityKey = CityKey.Normalize(profile.City);
        IReadOnlySet<Guid> events = new HashSet<Guid>();
        try
        {
            events = await places.GetEventTemplateIdsAsync(cityKey, nowUtc, nowUtc + EventHorizon, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Local events could not be loaded");
        }

        WeatherSnapshot? snapshot = null;
        try
        {
            snapshot = await weather.GetAsync(profile.City, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Weather could not be loaded");
        }

        if (snapshot is null)
            return new RealWorldContext(OutdoorWeather.Unknown, events, null);

        var verdict = WeatherAssessment.Assess(snapshot, fromLocal, toLocal);
        var advice = verdict.Outdoor == OutdoorWeather.Poor
            ? Text.Of($"{Capitalize(verdict.Reason!)}; açık hava görevleri şimdilik önerilmiyor.",
                $"{Capitalize(verdict.Reason!)}, so outdoor quests are paused for now.")
            : null;

        return new RealWorldContext(verdict.Outdoor, events, new WeatherDto(
            profile.City.Trim(), (int)Math.Round(snapshot.TemperatureC), WeatherAssessment.Describe(snapshot.WeatherCode),
            verdict.Outdoor, advice, snapshot.WeatherCode));
    }

    private static string Capitalize(string text) => char.ToUpper(text[0], Language.CurrentCulture) + text[1..];
}
