using LifeQuest.Domain.Common;
using LifeQuest.Domain.Community;
using LifeQuest.Domain.Experiments;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using static LifeQuest.Domain.Tests.TestData;

namespace LifeQuest.Domain.Tests;

public sealed class QuestPlanTests
{
    [Fact]
    public void Only_accepted_quests_can_be_planned_within_their_completion_window()
    {
        var quest = UserQuestTests.NewQuest();
        Assert.False(quest.Plan(UtcNow.AddDays(1), UtcNow).Succeeded);

        quest.Accept(UtcNow);
        Assert.True(quest.Plan(UtcNow.AddDays(2), UtcNow).Succeeded);
        Assert.Equal(UtcNow.AddDays(2), quest.PlannedAt);

        Assert.False(quest.Plan(UtcNow.AddDays(-1), UtcNow).Succeeded);
        Assert.False(quest.Plan(quest.ExpiresAt.AddMinutes(1), UtcNow).Succeeded);

        Assert.True(quest.Plan(null, UtcNow).Succeeded);
        Assert.Null(quest.PlannedAt);
    }
}

public sealed class ExperimentTests
{
    private static Experiment NewExperiment(double share = 0.5)
        => Experiment.Create("Sevdiğini tekrarla 0,8", "Tekrar muafiyeti north-star'ı artırır.",
            new Dictionary<string, double> { [nameof(RecommendationWeights.LovedRepeatNovelty)] = 0.8 }, share, "admin@example.com").Data!;

    [Fact]
    public void Lifecycle_is_draft_running_stopped_then_a_single_outcome()
    {
        var experiment = NewExperiment();

        Assert.False(experiment.Stop(UtcNow).Succeeded);
        Assert.False(experiment.Adopt().Succeeded);
        Assert.True(experiment.Start(UtcNow).Succeeded);
        Assert.False(experiment.Start(UtcNow).Succeeded);
        Assert.False(experiment.Adopt().Succeeded);
        Assert.True(experiment.Stop(UtcNow.AddDays(14)).Succeeded);
        Assert.True(experiment.Adopt().Succeeded);
        Assert.False(experiment.Discard().Succeeded);
        Assert.Equal(ExperimentOutcome.Adopted, experiment.Outcome);
    }

    [Fact]
    public void Invalid_overrides_or_share_are_rejected()
    {
        Assert.False(Experiment.Create("x", "y", new Dictionary<string, double> { ["Risk"] = 5 }, 0.5, "a").Succeeded);
        Assert.False(Experiment.Create("x", "y", new Dictionary<string, double>(), 0.5, "a").Succeeded);
        Assert.False(Experiment.Create("x", "y", new Dictionary<string, double> { ["Risk"] = 0.2 }, 0.95, "a").Succeeded);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.2)]
    public void Assignment_is_deterministic_and_matches_the_share(double share)
    {
        var experimentId = Guid.NewGuid();
        var users = Enumerable.Range(0, 10_000).Select(_ => Guid.NewGuid()).ToList();

        var treatment = users.Count(u => ExperimentAssignment.VariantFor(u, experimentId, share) == ExperimentVariant.Treatment);
        Assert.InRange(treatment / 10_000d, share - 0.03, share + 0.03);
        Assert.All(users.Take(100), u => Assert.Equal(
            ExperimentAssignment.VariantFor(u, experimentId, share), ExperimentAssignment.VariantFor(u, experimentId, share)));
    }

    [Fact]
    public void Different_experiments_assign_users_independently()
    {
        var users = Enumerable.Range(0, 2_000).Select(_ => Guid.NewGuid()).ToList();
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();

        var same = users.Count(u => ExperimentAssignment.VariantFor(u, first, 0.5) == ExperimentAssignment.VariantFor(u, second, 0.5));
        Assert.InRange(same / 2_000d, 0.45, 0.55);
    }
}

public sealed class QuestIdeaTests
{
    private static QuestIdea NewIdea() => QuestIdea.Submit(Guid.NewGuid(), "Mahalle kütüphanesi turu",
        "Mahallendeki kütüphaneyi ziyaret et ve bir rafı baştan sona incele.", LifeCategory.Learning, 45, CostBand.Free,
        false, ["Riskli ifade içeriyor", "Riskli ifade içeriyor"], UtcNow);

    [Fact]
    public void An_idea_is_reviewed_once()
    {
        var idea = NewIdea();
        Assert.Single(idea.Flags);

        var templateId = Guid.NewGuid();
        Assert.True(idea.Accept(templateId, "admin@example.com", null, UtcNow).Succeeded);
        Assert.Equal(IdeaStatus.Accepted, idea.Status);
        Assert.Equal(templateId, idea.TemplateId);
        Assert.False(idea.Reject("admin@example.com", "Geç kaldı", UtcNow).Succeeded);
    }

    [Fact]
    public void Rejection_keeps_the_note_for_the_user()
    {
        var idea = NewIdea();
        Assert.True(idea.Reject("admin@example.com", "Benzer bir deneyim zaten katalogda var.", UtcNow).Succeeded);
        Assert.Equal("Benzer bir deneyim zaten katalogda var.", idea.ReviewNote);
    }
}
