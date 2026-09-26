using System.Globalization;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.Formatting;

/// <summary>Web'deki core/labels/format.ts karşılığı; her çağrı o anki dilde biçimler.</summary>
public static class Format
{
    private static Strings S => Localizer.Instance.S;

    /// <summary>45–90 dk / 45–90 min, 1–2 sa / 1–2 h gibi okunabilir süre aralığı.</summary>
    public static string Duration(int minMinutes, int maxMinutes)
    {
        var (min, hour) = (S.Format.Min, S.Format.Hour);
        if (maxMinutes < 60)
            return minMinutes == maxMinutes ? $"{maxMinutes} {min}" : $"{minMinutes}–{maxMinutes} {min}";

        static string Hours(int m) => (m / 60.0).ToString("0.#", Lang.Culture);

        if (minMinutes < 60)
            return $"{minMinutes} {min} – {Hours(maxMinutes)} {hour}";
        return minMinutes == maxMinutes ? $"{Hours(maxMinutes)} {hour}" : $"{Hours(minMinutes)}–{Hours(maxMinutes)} {hour}";
    }

    /// <summary>"3 gün kaldı" / "3 days left", "Süresi doldu" / "Expired".</summary>
    public static string Remaining(DateTime expiresAtUtc, DateTime? nowUtc = null)
    {
        var diff = expiresAtUtc - (nowUtc ?? DateTime.UtcNow);
        if (diff <= TimeSpan.Zero)
            return S.Format.Expired;
        if (diff.TotalHours < 1)
            return S.Format.MinutesLeft(Math.Max(1, (int)Math.Round(diff.TotalMinutes, MidpointRounding.AwayFromZero)));
        if (diff.TotalHours < 24)
            return S.Format.HoursLeft((int)Math.Round(diff.TotalHours, MidpointRounding.AwayFromZero));
        return S.Format.DaysLeft((int)Math.Round(diff.TotalHours / 24, MidpointRounding.AwayFromZero));
    }

    /// <summary>"26 Eylül" / "26 September" (yerel saatle).</summary>
    public static string Date(DateTime utc, string pattern = "d MMMM") => Local(utc).ToString(pattern, Lang.Culture);

    public static string Date(DateOnly date, string pattern = "d MMMM") => date.ToString(pattern, Lang.Culture);

    /// <summary>"26 Eylül Cumartesi" / "Saturday 26 September".</summary>
    public static string LongDate(System.DateTime localNow)
        => localNow.ToString(Lang.Current == AppLanguage.En ? "dddd d MMMM" : "d MMMM dddd", Lang.Culture);

    public static string DateAndTime(System.DateTime utc) => Local(utc).ToString("d MMMM HH:mm", Lang.Culture);

    public static string Time(int hour) => $"{hour:00}:00";

    public static string Number(int value) => value.ToString("N0", Lang.Culture);

    public static string Percent(double ratio) => S.Format.Percent((int)Math.Round(ratio * 100, MidpointRounding.AwayFromZero));

    public static string Greeting(System.DateTime localNow)
    {
        var g = S.Format.Greetings;
        return localNow.Hour switch
        {
            < 6 => g.Night,
            < 12 => g.Morning,
            < 18 => g.Day,
            _ => g.Evening
        };
    }

    private static DateTime Local(DateTime utc)
        => (utc.Kind == DateTimeKind.Unspecified ? System.DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc).ToLocalTime();
}
