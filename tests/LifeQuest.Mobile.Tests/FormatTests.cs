using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Formatting;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Tests;

public sealed class FormatTests
{
    [Theory]
    [InlineData(AppLanguage.Tr, 45, 90, "45 dk – 1,5 sa")]
    [InlineData(AppLanguage.En, 45, 90, "45 min – 1.5 h")]
    [InlineData(AppLanguage.Tr, 20, 30, "20–30 dk")]
    [InlineData(AppLanguage.En, 30, 30, "30 min")]
    [InlineData(AppLanguage.Tr, 60, 120, "1–2 sa")]
    [InlineData(AppLanguage.En, 120, 120, "2 h")]
    public void Duration_matches_the_web(AppLanguage language, int min, int max, string expected)
    {
        using var _ = new LanguageScope(language);
        Assert.Equal(expected, Format.Duration(min, max));
    }

    [Fact]
    public void Remaining_time_is_rounded_and_pluralised()
    {
        var now = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
        using (new LanguageScope(AppLanguage.En))
        {
            Assert.Equal("1 day left", Format.Remaining(now.AddHours(30), now));
            Assert.Equal("3 hours left", Format.Remaining(now.AddHours(3), now));
            Assert.Equal("1 minute left", Format.Remaining(now.AddSeconds(10), now));
            Assert.Equal("Expired", Format.Remaining(now.AddMinutes(-1), now));
        }

        using (new LanguageScope(AppLanguage.Tr))
            Assert.Equal("2 gün kaldı", Format.Remaining(now.AddHours(48), now));
    }

    [Theory]
    [InlineData(5, "Good night")]
    [InlineData(9, "Good morning")]
    [InlineData(14, "Good afternoon")]
    [InlineData(20, "Good evening")]
    public void Greeting_depends_on_the_hour(int hour, string expected)
    {
        using var _ = new LanguageScope(AppLanguage.En);
        Assert.Equal(expected, Format.Greeting(new DateTime(2026, 9, 26, hour, 0, 0)));
    }

    [Fact]
    public void Labels_come_from_the_dictionary()
    {
        using (new LanguageScope(AppLanguage.En))
        {
            Assert.Equal("Movement", Labels.Category(LifeCategory.Fitness).Label);
            Assert.Equal("Free", Labels.Cost(CostBand.Free).Label);
            Assert.Equal("Chill", Labels.Radius(DiscoveryRadius.Chill).Label);
            Assert.Equal("1–2 h", Labels.WeeklyTime(120).Short);
        }

        using (new LanguageScope(AppLanguage.Tr))
            Assert.Equal("Hareket", Labels.Category(LifeCategory.Fitness).Label);
    }

    [Theory]
    [InlineData(null, "tr-TR", AppLanguage.Tr)]
    [InlineData(null, "en-US", AppLanguage.En)]
    [InlineData(null, "de-DE", AppLanguage.En)]
    [InlineData("tr", "en-US", AppLanguage.Tr)]
    [InlineData("en", "tr-TR", AppLanguage.En)]
    public void Language_comes_from_the_saved_choice_then_the_device(string? stored, string device, AppLanguage expected)
        => Assert.Equal(expected, Lang.Detect(stored, device));
}
