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

    /// <summary>
    /// Görev günü: yerel saatle <paramref name="dayStartHour"/>'da başlar. Gece 00:00–04:00 arası yapılanlar
    /// kullanıcının algısındaki gibi önceki güne sayılır ("dün gece tamamladım").
    /// </summary>
    public static DateOnly QuestDay(DateTime localNow, int dayStartHour)
        => DateOnly.FromDateTime(localNow.AddHours(-dayStartHour));

    /// <summary>Görev gününün bitişini (ertesi gün <paramref name="dayStartHour"/>) UTC olarak döner.</summary>
    public static DateTime EndOfQuestDayUtc(DateOnly questDay, TimeZoneInfo timeZone, int dayStartHour = 0)
    {
        var nextStart = questDay.AddDays(1).ToDateTime(new TimeOnly(dayStartHour, 0), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(nextStart, timeZone);
    }
}
