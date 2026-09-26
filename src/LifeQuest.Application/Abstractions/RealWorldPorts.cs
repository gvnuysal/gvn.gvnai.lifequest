using LifeQuest.Domain.RealWorld;

namespace LifeQuest.Application.Abstractions;

/// <summary>
/// Şehir adına göre hava durumu (Infrastructure'da Open-Meteo, önbellekli). Servise yalnızca şehir adı gider; kullanıcı
/// kimliği ya da kesin konum gönderilmez. Alınamazsa null döner ve öneriler havadan etkilenmez.
/// </summary>
public interface IWeatherProvider
{
    Task<WeatherSnapshot?> GetAsync(string city, CancellationToken cancellationToken);
}
