using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.RealWorld;
using LifeQuest.Domain.Recommendations;
using static LifeQuest.Domain.Tests.TestData;

namespace LifeQuest.Domain.Tests;

public sealed class RealWorldEngineTests
{
    private static readonly Guid Walk = Guid.NewGuid();
    private readonly QuestRecommendationEngine _engine = new(new RecommendationWeights());
    private static readonly DateTime Afternoon = new(2026, 9, 23, 15, 0, 0);

    [Fact]
    public void Bad_weather_filters_outdoor_quests_for_right_now_only()
    {
        var park = Candidate("park", LifeCategory.Fitness, [Walk], QuestType.Daily, minMinutes: 20, maxMinutes: 40) with { IsOutdoor = true };
        var gym = Candidate("gym", LifeCategory.Fitness, [Walk], QuestType.Daily, minMinutes: 20, maxMinutes: 40);
        var hike = Candidate("weekend-hike", LifeCategory.Fitness, [Walk]) with { IsOutdoor = true };
        var rainy = new RecommendationContext(Afternoon, UtcNow, 3, 1, RequireShortQuest: false, Weather: OutdoorWeather.Poor);

        var result = _engine.Recommend([park, gym, hike], Profile(new() { [Walk] = 1 }), RecommendationHistory.Empty, TasteGraph.Empty, rainy);

        // Haftalık açık hava görevi başka bir gün yapılabilir; günlük olan bugün önerilmez.
        Assert.Equal(["gym", "weekend-hike"], result.Items.Select(i => i.Candidate.Code).Order());
        Assert.Equal(1, result.FilteredOut["bad_weather"]);
    }

    [Fact]
    public void Local_event_lifts_a_linked_quest_and_is_the_first_reason()
    {
        var concert = Candidate("concert", LifeCategory.Culture, [Walk]);
        var museum = Candidate("museum", LifeCategory.Culture, [Walk]);
        var context = new RecommendationContext(Afternoon, UtcNow, 1, 1, RequireShortQuest: false,
            LocalEventTemplateIds: new HashSet<Guid> { concert.TemplateId });

        var without = _engine.Recommend([museum, concert], Profile(new() { [Walk] = 1 }), RecommendationHistory.Empty, TasteGraph.Empty,
            context with { LocalEventTemplateIds = null });
        var with = _engine.Recommend([museum, concert], Profile(new() { [Walk] = 1 }), RecommendationHistory.Empty, TasteGraph.Empty, context);

        var top = Assert.Single(with.Items);
        Assert.Equal("concert", top.Candidate.Code);
        Assert.Equal(ReasonCode.LocalEvent, top.Reasons[0].Code);
        Assert.StartsWith("Şehrinde bu hafta", top.Explanation.Tr);
        Assert.StartsWith("Suggested because there's a related event in your city this week", top.Explanation.En);
        Assert.True(top.Score.Total > without.Items.Single().Score.Total);
    }

    [Fact]
    public void Good_weather_favours_outdoor_by_day_but_not_at_night()
    {
        var park = Candidate("park", LifeCategory.Fitness, [Walk]) with { IsOutdoor = true };
        var gym = Candidate("gym", LifeCategory.Fitness, [Walk]);
        var sunny = new RecommendationContext(Afternoon, UtcNow, 1, 1, RequireShortQuest: false, Weather: OutdoorWeather.Good);

        var day = _engine.Recommend([gym, park], Profile(new() { [Walk] = 1 }), RecommendationHistory.Empty, TasteGraph.Empty, sunny);
        Assert.Equal("park", day.Items.Single().Candidate.Code);
        Assert.Contains(day.Items.Single().Reasons, r => r.Code == ReasonCode.GoodWeather);

        var night = _engine.Recommend([park], Profile(new() { [Walk] = 1 }), RecommendationHistory.Empty, TasteGraph.Empty,
            sunny with { LocalNow = new DateTime(2026, 9, 23, 23, 0, 0) });
        Assert.DoesNotContain(night.Items.Single().Reasons, r => r.Code == ReasonCode.GoodWeather);
    }
}

public sealed class WeatherAssessmentTests
{
    private static readonly DateTime Noon = new(2026, 9, 23, 12, 0, 0);

    private static WeatherSnapshot Snapshot(params (int Hour, double Temp, int Rain, int Code)[] hours)
        => new("Test", DateTime.UtcNow, 20, 0, 5, true,
            hours.Select(h => new HourlyWeather(Noon.Date.AddHours(h.Hour), h.Temp, h.Rain, h.Code, 10)).ToList());

    [Fact]
    public void Rain_later_in_the_window_makes_it_poor_but_rain_outside_does_not()
    {
        var snapshot = Snapshot((12, 20, 10, 1), (15, 19, 80, 61), (22, 18, 90, 63));
        Assert.Equal(OutdoorWeather.Poor, WeatherAssessment.Assess(snapshot, Noon, Noon.AddHours(4)).Outdoor);
        Assert.Equal(OutdoorWeather.Good, WeatherAssessment.Assess(snapshot, Noon, Noon.AddHours(2)).Outdoor);
        Assert.Equal("yağmur bekleniyor", WeatherAssessment.Assess(snapshot, Noon, Noon.AddHours(4)).Reason);
    }

    [Theory]
    [InlineData(37, 0, 0, "aşırı sıcak bekleniyor")]
    [InlineData(1, 0, 0, "hava çok soğuk")]
    [InlineData(10, 0, 95, "fırtına bekleniyor")]
    [InlineData(-2, 20, 73, "fırtına bekleniyor")]
    public void Extremes_are_poor_with_a_reason(double temp, int rain, int code, string reason)
    {
        var verdict = WeatherAssessment.Assess(Snapshot((12, temp, rain, code)), Noon, Noon.AddHours(1));
        Assert.Equal(OutdoorWeather.Poor, verdict.Outdoor);
        if (code != 73) Assert.Equal(reason, verdict.Reason);
    }

    [Fact]
    public void Missing_hours_are_unknown_and_the_daily_window_covers_daytime()
    {
        Assert.Equal(OutdoorWeather.Unknown, WeatherAssessment.Assess(Snapshot(), Noon, Noon.AddHours(3)).Outdoor);
        Assert.Equal((Noon.Date.AddHours(9), Noon.Date.AddHours(21)), WeatherAssessment.DailyWindow(Noon.Date.AddHours(5)));
        Assert.Equal((Noon, Noon.Date.AddHours(21)), WeatherAssessment.DailyWindow(Noon));
        var late = Noon.Date.AddHours(22);
        Assert.Equal((late, late.AddHours(3)), WeatherAssessment.DailyWindow(late));
    }
}

public sealed class LocalPlaceTests
{
    [Theory]
    [InlineData("İstanbul")]
    [InlineData("istanbul")]
    [InlineData("  ISTANBUL ")]
    [InlineData("Istanbul")]
    public void City_spellings_share_one_key(string city) => Assert.Equal("istanbul", CityKey.Normalize(city));

    [Fact]
    public void Events_need_dates_and_relevance_follows_the_window()
    {
        var template = Guid.NewGuid();
        var start = new DateTime(2026, 10, 3, 17, 0, 0, DateTimeKind.Utc);
        Assert.False(LocalPlace.Create(new LocalPlaceDraft(LocalPlaceKind.Event, "İzmir", "Konser", null, null, null, null, null, [template]), "a@x").Succeeded);
        Assert.False(LocalPlace.Create(new LocalPlaceDraft(LocalPlaceKind.Venue, "İzmir", "Park", null, null, null, null, null, []), "a@x").Succeeded);

        var concert = LocalPlace.Create(new LocalPlaceDraft(LocalPlaceKind.Event, " İzmir ", "Konser", null, "https://x.com", null,
            start, start.AddHours(2), [template, template]), "a@x").Data!;
        Assert.Equal("izmir", concert.CityKey);
        Assert.Single(concert.TemplateIds);
        Assert.True(concert.IsRelevantBetween(start.AddDays(-3), start.AddDays(4)));
        Assert.False(concert.IsRelevantBetween(start.AddDays(1), start.AddDays(8)));

        var venue = LocalPlace.Create(new LocalPlaceDraft(LocalPlaceKind.Venue,
            "İzmir", "Kordon", null, null, null, start, start, [template]), "a@x").Data!;
        Assert.Null(venue.StartsAt);
        Assert.True(venue.IsRelevantBetween(start.AddYears(1), start.AddYears(2)));
    }
}
