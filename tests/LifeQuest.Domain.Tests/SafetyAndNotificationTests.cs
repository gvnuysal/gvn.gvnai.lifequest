using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Recommendations;
using static LifeQuest.Domain.Tests.TestData;

namespace LifeQuest.Domain.Tests;

public sealed class SafetyFilterTests
{
    private static readonly Guid Walk = Guid.NewGuid();
    private readonly QuestRecommendationEngine _engine = new(new RecommendationWeights());

    [Fact]
    public void Quests_above_the_users_effort_limit_are_never_offered()
    {
        var run = Candidate("run", LifeCategory.Fitness, [Walk]) with { Effort = PhysicalEffort.Vigorous };
        var stretch = Candidate("stretch", LifeCategory.Fitness, [Walk]) with { Effort = PhysicalEffort.Light };

        var result = _engine.Recommend([run, stretch],
            Profile(new() { [Walk] = 1 }) with { MaxEffort = PhysicalEffort.Light },
            RecommendationHistory.Empty, TasteGraph.Empty, Context());

        Assert.Equal(["stretch"], result.Items.Select(i => i.Candidate.Code));
        Assert.Equal(1, result.FilteredOut["effort_limit"]);
    }

    [Fact]
    public void Outdoor_quests_are_not_suggested_for_right_now_at_night()
    {
        var nightWalk = Candidate("night-walk", LifeCategory.Fitness, [Walk], QuestType.Daily, minMinutes: 15, maxMinutes: 30) with { IsOutdoor = true };
        var indoor = Candidate("indoor", LifeCategory.Fitness, [Walk], QuestType.Daily, minMinutes: 10, maxMinutes: 15);
        var weeklyOutdoor = Candidate("weekend-hike", LifeCategory.Fitness, [Walk]) with { IsOutdoor = true };
        var night = new RecommendationContext(new DateTime(2026, 9, 23, 23, 30, 0), UtcNow, 3, 1, RequireShortQuest: false);

        var daily = _engine.Recommend([nightWalk, indoor, weeklyOutdoor], Profile(new() { [Walk] = 1 }),
            RecommendationHistory.Empty, TasteGraph.Empty, night);
        var rightNow = _engine.Recommend([weeklyOutdoor], Profile(new() { [Walk] = 1 }),
            RecommendationHistory.Empty, TasteGraph.Empty, night with { AvailableMinutes = 120 });

        // Haftalık açık hava görevi gece önerilebilir (hafta içinde gündüz yapılır), ama "şimdi" bağlamında önerilmez.
        Assert.Equal(["indoor", "weekend-hike"], daily.Items.Select(i => i.Candidate.Code).Order());
        Assert.True(rightNow.IsEmpty);
        Assert.Equal(1, rightNow.FilteredOut["outdoor_at_night"]);
    }
}

public sealed class CatalogSafetyRulesTests
{
    private static QuestTemplateSpec Spec(
        QuestType type = QuestType.Weekly, int min = 45, int max = 90, bool outdoor = false, DayPart dayParts = DayPart.Any,
        double risk = 0, PhysicalEffort effort = PhysicalEffort.Light, string title = "Geçerli bir başlık",
        string description = "Yeterince uzun ve anlamlı bir quest açıklaması burada yer alıyor.")
        => new("code", title, description, type, Difficulty.Easy, LifeCategory.Culture, null, min, max, CostBand.Free,
            dayParts, false, outdoor, 7, risk, [Guid.NewGuid()], effort, false);

    [Fact]
    public void Valid_template_passes()
        => Assert.Empty(CatalogSafetyRules.ValidateTemplate(Spec(outdoor: true, dayParts: DayPart.Morning)));

    [Theory]
    [InlineData("outdoor-night")]
    [InlineData("risky")]
    [InlineData("long-daily")]
    [InlineData("short-epic")]
    [InlineData("vigorous-without-risk")]
    [InlineData("short-description")]
    public void Unsafe_or_inconsistent_templates_are_flagged(string scenario)
    {
        var spec = scenario switch
        {
            "outdoor-night" => Spec(outdoor: true, dayParts: DayPart.Evening | DayPart.Night),
            "risky" => Spec(risk: 0.5),
            "long-daily" => Spec(QuestType.Daily, 20, 45),
            "short-epic" => Spec(QuestType.Epic, 60, 120),
            "vigorous-without-risk" => Spec(effort: PhysicalEffort.Vigorous),
            _ => Spec(description: "Çok kısa.")
        };

        Assert.NotEmpty(CatalogSafetyRules.ValidateTemplate(spec));
    }

    [Fact]
    public void Editorial_update_bumps_version_only_when_something_changes()
    {
        var spec = Spec();
        var template = QuestTemplate.Create(spec);

        Assert.False(template.ApplyEditorial(spec with { InterestIds = spec.InterestIds.ToList() }));
        Assert.Equal(1, template.Version);

        Assert.True(template.ApplyEditorial(spec with { Effort = PhysicalEffort.Moderate }));
        Assert.Equal(2, template.Version);
        Assert.Equal(PhysicalEffort.Moderate, template.Effort);
    }
}

public sealed class WeeklySummaryComposerTests
{
    [Fact]
    public void Active_week_celebrates_experiences_and_new_areas()
    {
        var (_, message) = WeeklySummaryComposer.Compose(
            new WeeklyStats(3, 420, [LifeCategory.Culture, LifeCategory.Fitness], LifeCategory.Culture, 1));

        Assert.Contains("3 gerçek deneyim", message.Tr);
        Assert.Contains("420 XP", message.Tr);
        Assert.Contains("Kültür ve Hareket alanında ilk adımını attın", message.Tr);
        Assert.Contains("En çok Kültür", message.Tr);
        Assert.Equal("This week you had 3 real-life experiences and earned 420 XP. You took your first step in Culture and Movement. "
                     + "You spent the most time on Culture.", message.En);
    }

    [Fact]
    public void Quiet_week_is_never_framed_as_a_loss()
    {
        var (_, quiet) = WeeklySummaryComposer.Compose(new WeeklyStats(0, 0, [], null, 0));
        var (_, pending) = WeeklySummaryComposer.Compose(new WeeklyStats(0, 0, [], null, 2));

        foreach (var message in new[] { quiet.Tr, pending.Tr })
        {
            Assert.DoesNotContain("kaybettin", message);
            Assert.DoesNotContain("seri", message);
            Assert.DoesNotContain("girmedin", message);
        }
        Assert.Contains("Acele yok", pending.Tr);
        Assert.DoesNotContain("lost", pending.En + quiet.En);
    }

    [Fact]
    public void Admin_role_can_be_granted_once()
    {
        var account = UserAccount.Register("a@b.com", "hash", "Ali", 1990, UtcNow).Data!;
        Assert.True(account.GrantRole(UserRoles.Admin));
        Assert.False(account.GrantRole(UserRoles.Admin));
        Assert.Equal(UserRoles.Admin, account.Role);
    }
}

public sealed class InterestCoverageTests
{
    private static readonly Guid Dance = Guid.NewGuid();
    private static readonly Guid Yoga = Guid.NewGuid();

    private static QuestTemplateSpec Spec(Guid interest, int maxMinutes)
        => new($"q-{Guid.NewGuid():N}", "Geçerli bir başlık", "Yeterince uzun ve anlamlı bir quest açıklaması burada yer alıyor.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Fitness, null, 20, maxMinutes, CostBand.Free, DayPart.Any,
            false, false, 7, 0, [interest], PhysicalEffort.Light, false);

    [Fact]
    public void Thin_interests_and_interests_without_a_short_quest_are_reported()
    {
        var specs = new[] { Spec(Dance, 90), Spec(Dance, 120), Spec(Yoga, 30), Spec(Yoga, 30), Spec(Yoga, 45), Spec(Yoga, 90) };

        var violations = CatalogSafetyRules.ValidateInterestCoverage(specs,
            new Dictionary<Guid, string> { [Dance] = "Dans", [Yoga] = "Yoga" });

        Assert.Equal(["Dans: 2 görev (en az 4).", "Dans: 60 dakikalık kısa görev yok."], violations);
    }
}
