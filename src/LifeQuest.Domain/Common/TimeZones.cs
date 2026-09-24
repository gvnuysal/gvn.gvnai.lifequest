namespace LifeQuest.Domain.Common;

public static class TimeZones
{
    public const string Default = "Europe/Istanbul";

    public static bool IsValid(string? timeZoneId)
        => !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    public static TimeZoneInfo Resolve(string? timeZoneId)
        => timeZoneId is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz)
            ? tz
            : TimeZoneInfo.Utc;

    /// <summary>Kullanıcının yerel gününün bitişini (bir sonraki gece yarısı) UTC olarak döner.</summary>
    public static DateTime EndOfLocalDayUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var nextMidnight = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(nextMidnight, timeZone);
    }
}
