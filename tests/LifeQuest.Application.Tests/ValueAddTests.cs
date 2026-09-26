using LifeQuest.Application.Calendar;
using LifeQuest.Application.Experiments;
using LifeQuest.Application.Safety;

namespace LifeQuest.Application.Tests;

public sealed class IcsCalendarTests
{
    private static readonly DateTime Start = new(2026, 10, 3, 7, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Builds_a_single_utc_event_without_location()
    {
        var ics = IcsCalendar.Build(new CalendarEvent(Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "Sahafta bir saat", "Bir kitabın ilk sayfasını oku.", Start, TimeSpan.FromMinutes(90), Start.AddDays(-1)));

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains("DTSTART:20261003T073000Z\r\n", ics);
        Assert.Contains("DURATION:PT90M\r\n", ics);
        Assert.Contains("UID:11111111222233334444555555555555@lifequest", ics);
        Assert.DoesNotContain("LOCATION", ics);
        Assert.DoesNotContain("\n\n", ics.Replace("\r\n", "\n").Replace("\n ", ""));
    }

    [Fact]
    public void Escapes_special_characters()
        => Assert.Equal(@"Kahve\, çay\; su\\ ve\nyeni satır", IcsCalendar.Escape("Kahve, çay; su\\ ve\nyeni satır"));

    [Fact]
    public void Long_lines_are_folded_at_75_octets_without_splitting_characters()
    {
        var ics = IcsCalendar.Build(new CalendarEvent(Guid.NewGuid(), new string('ş', 100), "Açıklama", Start,
            TimeSpan.FromHours(2), Start));

        foreach (var line in ics.Split("\r\n"))
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(line) <= 75, line);
        Assert.Contains("DURATION:PT2H", ics);
        Assert.Contains(new string('ş', 100), ics.Replace("\r\n ", ""));
    }
}

public sealed class ExperimentStatisticsTests
{
    private static List<UserExperimentStats> Users(int count, Func<int, int> meaningful)
        => Enumerable.Range(0, count)
            .Select(i => new UserExperimentStats(Guid.NewGuid(), 10, 4, 3, meaningful(i), 2, 1, 1, 8, 2))
            .ToList();

    [Fact]
    public void Clear_improvement_is_detected()
    {
        var control = ExperimentStatistics.Summarize(Users(50, i => i % 2 == 0 ? 2 : 4), weeks: 2);
        var treatment = ExperimentStatistics.Summarize(Users(50, i => i % 2 == 0 ? 6 : 8), weeks: 2);
        var comparison = ExperimentStatistics.Compare(control, treatment);

        Assert.Equal(1.5, control.NorthStar);
        Assert.Equal(3.5, treatment.NorthStar);
        Assert.Equal(2, comparison.Difference);
        Assert.True(comparison.CiLow > 0);
        Assert.Equal(1.333, comparison.RelativeLift);
        Assert.Equal(ExperimentVerdict.TreatmentBetter, ExperimentStatistics.Verdict(control, treatment, comparison));
        Assert.Equal(0.4, control.AcceptanceRate);
        Assert.Equal(4, control.AverageRating);
    }

    [Fact]
    public void Overlapping_results_mean_no_difference()
    {
        var control = ExperimentStatistics.Summarize(Users(60, i => i % 5), weeks: 1);
        var treatment = ExperimentStatistics.Summarize(Users(60, i => (i + 1) % 5), weeks: 1);
        var comparison = ExperimentStatistics.Compare(control, treatment);

        Assert.True(comparison.CiLow < 0 && comparison.CiHigh > 0);
        Assert.Equal(ExperimentVerdict.NoDifference, ExperimentStatistics.Verdict(control, treatment, comparison));
    }

    [Fact]
    public void Small_groups_are_insufficient_data_whatever_the_difference()
    {
        var control = ExperimentStatistics.Summarize(Users(10, _ => 0), weeks: 1);
        var treatment = ExperimentStatistics.Summarize(Users(10, _ => 9), weeks: 1);

        Assert.Equal(ExperimentVerdict.InsufficientData,
            ExperimentStatistics.Verdict(control, treatment, ExperimentStatistics.Compare(control, treatment)));
    }

    [Fact]
    public void Guardrail_flags_a_winner_that_users_reject_more_often()
    {
        static List<UserExperimentStats> WithSkips(int notInterested)
            => Enumerable.Range(0, 40).Select(_ => new UserExperimentStats(Guid.NewGuid(), 10, 4, 3, 3, 2, 1, notInterested, 8, 2)).ToList();

        var control = ExperimentStatistics.Summarize(WithSkips(1), weeks: 1);
        Assert.False(ExperimentStatistics.GuardrailBreached(control, ExperimentStatistics.Summarize(WithSkips(1), weeks: 1)));
        Assert.True(ExperimentStatistics.GuardrailBreached(control, ExperimentStatistics.Summarize(WithSkips(2), weeks: 1)));

        // Küçük gruplarda uyarı verilmez: gürültü karar değildir.
        var small = ExperimentStatistics.Summarize(WithSkips(5).Take(5).ToList(), weeks: 1);
        Assert.False(ExperimentStatistics.GuardrailBreached(control, small));
    }

    [Fact]
    public void Empty_variant_is_zero_not_an_error()
    {
        var empty = ExperimentStatistics.Summarize([], weeks: 0);
        Assert.Equal(0, empty.Users);
        Assert.Equal(0, empty.NorthStar);
        Assert.Null(empty.AverageRating);
    }
}

public sealed class ContentScreenTests
{
    [Theory]
    [InlineData("Detaylar için https://ornek.com adresine bak")]
    [InlineData("www.ornek.org üzerinden kayıt ol")]
    [InlineData("sitemiz etkinlik.com.tr adresinde")]
    public void Links_are_detected(string text) => Assert.True(ContentScreen.ContainsUrl(text));

    [Theory]
    [InlineData("Bana ali.veli@example.com adresinden yaz")]
    [InlineData("Katılmak için 0532 123 45 67'yi ara")]
    [InlineData("+90 (212) 555-12-34")]
    public void Contact_details_are_detected(string text) => Assert.True(ContentScreen.ContainsContactInfo(text));

    [Theory]
    [InlineData("Biraz yürüyüş yapıp 2 saat boyunca 3 farklı kafeyi dene.")]
    [InlineData("Saat 18.30'da başla, 45 dakikada bitir.")]
    [InlineData("Sonra.dan bakarız gibi yazım hataları link sayılmaz")]
    public void Innocent_text_is_not_flagged(string text)
    {
        Assert.False(ContentScreen.ContainsUrl(text));
        Assert.False(ContentScreen.ContainsContactInfo(text));
        Assert.False(ContentScreen.ContainsRiskyContent(text));
    }

    [Fact]
    public void Risky_phrases_are_detected_with_turkish_suffixes()
        => Assert.True(ContentScreen.ContainsRiskyContent("Gece yarısı ıssız bir sokakta yürü"));
}
