using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using static LifeQuest.Domain.Tests.TestData;

namespace LifeQuest.Domain.Tests;

public sealed class RecommendationEngineTests
{
    private static readonly Guid Coffee = Guid.NewGuid();
    private static readonly Guid CafeCulture = Guid.NewGuid();
    private static readonly Guid Cinema = Guid.NewGuid();
    private static readonly Guid Art = Guid.NewGuid();
    private static readonly Guid Photography = Guid.NewGuid();
    private static readonly Guid Walking = Guid.NewGuid();
    private static readonly Guid Reading = Guid.NewGuid();

    private static readonly TasteGraph Graph = new(
        [new InterestEdge(Coffee, CafeCulture, 0.9, 0.8)],
        [
            new InterestInfo(Coffee, "coffee", "Kahve"),
            new InterestInfo(CafeCulture, "cafe-culture", "Kafe Kültürü"),
            new InterestInfo(Art, "art", "Sanat")
        ]);

    private readonly QuestRecommendationEngine _engine = new(new RecommendationWeights());

    /// <summary>
    /// Analiz §21: 2 saat, orta bütçe, Explore. İlgi: kahve 0.9, sinema 0.8, sanat 0.4; son aktivitelerin ikisi kahve.
    /// Kahveci yüksek Interest alır ama RepetitionPenalty yükselir; sergi yüksek Novelty/Diversity ile öne geçer.
    /// </summary>
    [Fact]
    public void Paper_scenario_exhibition_outranks_new_cafe_after_coffee_streak()
    {
        var newCafe = Candidate("new-cafe", LifeCategory.Explorer, [Coffee], maxMinutes: 90);
        var shortFilm = Candidate("short-film", LifeCategory.Culture, [Cinema], minMinutes: 60, maxMinutes: 100);
        var exhibition = Candidate("photo-exhibition", LifeCategory.Culture, [Art, Photography], minMinutes: 60, maxMinutes: 120, cost: CostBand.Free);
        var walkPhoto = Candidate("walk-photo", LifeCategory.Fitness, [Walking, Photography], maxMinutes: 45, cost: CostBand.Free);

        var pastCoffee1 = Candidate("coffee-tasting", LifeCategory.Explorer, [Coffee]);
        var pastCoffee2 = Candidate("latte-art", LifeCategory.Explorer, [Coffee]);
        var pastReading = Candidate("ten-pages", LifeCategory.Learning, [Reading]);

        var profile = Profile(new() { [Coffee] = 0.9, [Cinema] = 0.8, [Art] = 0.4 });
        var history = History([Completed(pastCoffee1, 1), Completed(pastCoffee2, 2), Completed(pastReading, 3)]);

        var result = _engine.Recommend([newCafe, shortFilm, exhibition, walkPhoto], profile, history, Graph,
            Context(count: 4, availableMinutes: 120));

        var codes = result.Items.Select(i => i.Candidate.Code).ToList();
        Assert.True(codes.IndexOf("photo-exhibition") < codes.IndexOf("new-cafe"), string.Join(", ", codes));

        var cafe = result.Items.Single(i => i.Candidate.Code == "new-cafe");
        Assert.True(cafe.Score.Interest >= 0.9);
        Assert.True(cafe.Score.Repetition > 0.5);

        var exhibitionItem = result.Items.Single(i => i.Candidate.Code == "photo-exhibition");
        Assert.Equal(1.0, exhibitionItem.Score.Novelty);
        Assert.Contains(exhibitionItem.Reasons, r => r.Code == ReasonCode.Diversification);
        Assert.StartsWith("Son aktivitelerinde Keşif ağırlığı olduğu", exhibitionItem.Explanation);
        Assert.EndsWith("bu kez bir Kültür quest'i önerdik.", exhibitionItem.Explanation);
    }

    [Fact]
    public void Template_in_cooldown_is_filtered_out()
    {
        var quest = Candidate("q", LifeCategory.Culture, [Art], cooldownDays: 14);
        var other = Candidate("other", LifeCategory.Culture, [Art]);

        var result = _engine.Recommend([quest, other], Profile(new() { [Art] = 0.8 }),
            History([Completed(quest, daysAgo: 3)]), Graph, Context());

        Assert.DoesNotContain(result.Items, i => i.Candidate.Code == "q");
        Assert.Equal(1, result.FilteredOut["cooldown"]);
    }

    [Fact]
    public void Over_budget_and_city_required_quests_are_filtered_out()
    {
        var expensive = Candidate("expensive", LifeCategory.Culture, [Art], cost: CostBand.High);
        var needsCity = Candidate("needs-city", LifeCategory.Culture, [Art], requiresCity: true);
        var ok = Candidate("ok", LifeCategory.Culture, [Art]);

        var result = _engine.Recommend([expensive, needsCity, ok],
            Profile(new() { [Art] = 0.8 }, budget: CostBand.Low, hasCity: false), RecommendationHistory.Empty, Graph, Context());

        Assert.Equal(["ok"], result.Items.Select(i => i.Candidate.Code));
        Assert.Equal(1, result.FilteredOut["over_budget"]);
        Assert.Equal(1, result.FilteredOut["requires_city"]);
    }

    [Fact]
    public void Available_minutes_filter_out_quests_that_cannot_fit()
    {
        var longQuest = Candidate("long", LifeCategory.Culture, [Art], minMinutes: 180, maxMinutes: 240);
        var fits = Candidate("fits", LifeCategory.Culture, [Art], minMinutes: 30, maxMinutes: 60);

        var result = _engine.Recommend([longQuest, fits], Profile(new() { [Art] = 0.8 }),
            RecommendationHistory.Empty, Graph, Context(availableMinutes: 120));

        var item = Assert.Single(result.Items);
        Assert.Equal("fits", item.Candidate.Code);
        Assert.Contains(item.Reasons, r => r.Code == ReasonCode.FitsAvailableTime);
    }

    [Fact]
    public void When_everything_is_filtered_result_is_empty_not_an_error()
    {
        var result = _engine.Recommend([Candidate("expensive", LifeCategory.Culture, [Art], cost: CostBand.High)],
            Profile(new(), budget: CostBand.Free), RecommendationHistory.Empty, Graph, Context());

        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.EligibleCount);
    }

    [Fact]
    public void Empty_catalog_returns_empty_result()
    {
        var result = _engine.Recommend([], Profile(new()), RecommendationHistory.Empty, Graph, Context());
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Not_interested_skip_blocks_the_template_for_two_weeks()
    {
        var quest = Candidate("q", LifeCategory.Culture, [Art]);

        var recent = _engine.Recommend([quest], Profile(new() { [Art] = 1 }),
            History([Skipped(quest, SkipReason.NotInterested, daysAgo: 5)]), Graph, Context());
        var old = _engine.Recommend([quest], Profile(new() { [Art] = 1 }),
            History([Skipped(quest, SkipReason.NotInterested, daysAgo: 20)]), Graph, Context());

        Assert.True(recent.IsEmpty);
        Assert.Single(old.Items);
    }

    [Fact]
    public void Too_expensive_skip_is_friction_not_disinterest()
    {
        var quest = Candidate("q", LifeCategory.Culture, [Art], cost: CostBand.Medium);
        var skippedOther = Candidate("other", LifeCategory.Social, [Reading]);

        var result = _engine.Recommend([quest], Profile(new() { [Art] = 1 }, budget: CostBand.High),
            History([Skipped(skippedOther, SkipReason.TooExpensive, daysAgo: 2)]), Graph, Context());

        var item = Assert.Single(result.Items);
        Assert.Equal(1.0, item.Score.Interest);
        Assert.True(item.Score.Friction >= 0.3);
    }

    [Fact]
    public void Second_pick_from_same_category_gets_diversity_penalty()
    {
        var a = Candidate("a", LifeCategory.Culture, [Art]);
        var b = Candidate("b", LifeCategory.Culture, [Cinema]);

        var result = _engine.Recommend([a, b], Profile(new() { [Art] = 0.9, [Cinema] = 0.9 }, DiscoveryRadius.Chill),
            RecommendationHistory.Empty, Graph, Context(count: 2));

        Assert.Equal(1.0, result.Items[0].Score.Diversity);
        Assert.Equal(0.5, result.Items[1].Score.Diversity);
    }

    [Fact]
    public void Adjacent_interest_from_taste_graph_is_used_and_explained()
    {
        var cafeTour = Candidate("cafe-tour", LifeCategory.Explorer, [CafeCulture]);

        var result = _engine.Recommend([cafeTour], Profile(new() { [Coffee] = 0.9 }),
            RecommendationHistory.Empty, Graph, Context());

        var item = Assert.Single(result.Items);
        Assert.Equal(Math.Round(0.9 * 0.9 * 0.8 * 0.6, 4), item.Score.Interest);
        var reason = Assert.Single(item.Reasons, r => r.Code == ReasonCode.AdjacentInterest);
        Assert.Contains("Kahve", reason.Text);
        Assert.Contains("Kafe Kültürü", reason.Text);
    }

    [Fact]
    public void First_slot_is_a_short_quest_when_requested()
    {
        var weekly = Candidate("weekly", LifeCategory.Culture, [Art], QuestType.Weekly);
        var daily = Candidate("daily", LifeCategory.Learning, [Reading], QuestType.Daily, Difficulty.Easy, minMinutes: 10, maxMinutes: 15);

        var result = _engine.Recommend([weekly, daily], Profile(new() { [Art] = 1.0 }),
            RecommendationHistory.Empty, Graph, Context(count: 2, requireShort: true));

        Assert.Equal("daily", result.Items[0].Candidate.Code);
    }

    [Fact]
    public void Chill_never_uses_an_exploration_slot_while_surprise_me_does()
    {
        var known = Enumerable.Range(0, 3).Select(i => Candidate($"known-{i}", LifeCategory.Culture, [Art])).ToList();
        // Bağlama zayıf uyan (sabah görevi, akşam saati) ve ilgiyle eşleşmeyen yeni alan: normal sıralamada
        // öne çıkmaz, ancak keşif slotu onu kontrollü şekilde dener.
        var unknown = Candidate("unknown", LifeCategory.Creativity, [Photography], QuestType.Daily,
            cost: CostBand.Medium, dayParts: DayPart.Morning);
        var history = History([Completed(Candidate("past", LifeCategory.Culture, [Art]), 30)]);

        var chill = _engine.Recommend([.. known, unknown], Profile(new() { [Art] = 0.9 }, DiscoveryRadius.Chill), history, Graph, Context());
        var surprise = _engine.Recommend([.. known, unknown], Profile(new() { [Art] = 0.9 }, DiscoveryRadius.SurpriseMe), history, Graph, Context());

        Assert.DoesNotContain(chill.Items, i => i.IsExploration);
        var exploration = Assert.Single(surprise.Items, i => i.IsExploration);
        Assert.Equal("unknown", exploration.Candidate.Code);
        Assert.Contains(exploration.Reasons, r => r.Code == ReasonCode.ExplorationPick);
    }

    [Fact]
    public void Exploration_slot_prefers_quests_adjacent_to_a_loved_interest()
    {
        var known = Enumerable.Range(0, 3).Select(i => Candidate($"known-{i}", LifeCategory.Explorer, [Coffee])).ToList();
        // İkisi de yeni alan ve ilgi eşleşmesi zayıf; biri Taste Graph'ta kahveye komşu (kafe kültürü).
        var adjacent = Candidate("cafe-culture-walk", LifeCategory.Culture, [CafeCulture], cost: CostBand.Medium);
        var random = Candidate("random-new", LifeCategory.Creativity, [Photography], cost: CostBand.Medium);
        var history = History([Completed(Candidate("past", LifeCategory.Explorer, [Coffee]), 30)]);

        var explorationDays = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var result = _engine.Recommend([.. known, adjacent, random],
                Profile(new() { [Coffee] = 0.9 }, DiscoveryRadius.Explore), history, Graph, Context(seed: seed));

            var exploration = result.Items.SingleOrDefault(i => i.IsExploration);
            if (exploration is not null)
            {
                explorationDays++;
                Assert.Equal("cafe-culture-walk", exploration.Candidate.Code);
            }
        }

        // Dengeli modda keşif slotu her gün değil, tohumlu olasılıkla açılır.
        Assert.InRange(explorationDays, 3, 17);
    }

    [Fact]
    public void Same_input_and_seed_produce_same_recommendations()
    {
        var candidates = Enumerable.Range(0, 10)
            .Select(i => Candidate($"q{i}", LifeCategories.All[i % 6], [i % 2 == 0 ? Art : Photography]))
            .ToList();
        var profile = Profile(new() { [Art] = 0.3 }, DiscoveryRadius.SurpriseMe);

        var first = _engine.Recommend(candidates, profile, RecommendationHistory.Empty, Graph, Context(seed: 7));
        var second = _engine.Recommend(candidates, profile, RecommendationHistory.Empty, Graph, Context(seed: 7));

        Assert.Equal(first.Items.Select(i => i.Candidate.Code), second.Items.Select(i => i.Candidate.Code));
    }

    [Fact]
    public void Open_quests_are_not_offered_again()
    {
        var quest = Candidate("q", LifeCategory.Culture, [Art]);

        var result = _engine.Recommend([quest], Profile(new() { [Art] = 1 }),
            History([], open: [quest.TemplateId]), Graph, Context());

        Assert.True(result.IsEmpty);
        Assert.Equal(1, result.FilteredOut["already_open"]);
    }
}
