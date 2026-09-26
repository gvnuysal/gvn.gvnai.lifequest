using LifeQuest.Domain.Localization;
using System.Globalization;
using System.Text;

namespace LifeQuest.Application.Calendar;

public sealed record CalendarEvent(Guid Id, string Title, string Description, DateTime StartUtc, TimeSpan Duration, DateTime CreatedUtc);

/// <summary>
/// RFC 5545 (iCalendar) tek etkinlik üretir. Konum bilinçli olarak eklenmez (mahremiyet); zaman UTC yazılır,
/// takvim uygulaması kullanıcının yerel saatine çevirir.
/// </summary>
public static class IcsCalendar
{
    private const int MaxLineOctets = 75;

    public static string Build(CalendarEvent e)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            $"PRODID:-//LifeQuest//Quest//{Language.Current.ToUpperInvariant()}",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{e.Id:N}@lifequest",
            $"DTSTAMP:{Utc(e.CreatedUtc)}",
            $"DTSTART:{Utc(e.StartUtc)}",
            $"DURATION:{Duration(e.Duration)}",
            $"SUMMARY:{Escape(e.Title)}",
            $"DESCRIPTION:{Escape(e.Description + "\n\n" + Text.Of("LifeQuest ile planlandı.", "Planned with LifeQuest."))}",
            "TRANSP:OPAQUE",
            "END:VEVENT",
            "END:VCALENDAR"
        };

        var sb = new StringBuilder();
        foreach (var line in lines)
            Fold(sb, line);
        return sb.ToString();
    }

    public static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\n").Replace("\n", "\\n");

    private static string Utc(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Duration(TimeSpan d)
        => d.TotalMinutes >= 60 && d.Minutes == 0 ? $"PT{(int)d.TotalHours}H" : $"PT{(int)d.TotalMinutes}M";

    /// <summary>Satırlar 75 oktette katlanır; devam satırı tek boşlukla başlar. Çok baytlı karakterler bölünmez.</summary>
    private static void Fold(StringBuilder sb, string line)
    {
        var octets = 0;
        var limit = MaxLineOctets;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (octets + size > limit)
            {
                sb.Append("\r\n ");
                octets = 0;
                limit = MaxLineOctets - 1;
            }

            sb.Append(rune.ToString());
            octets += size;
        }

        sb.Append("\r\n");
    }
}
