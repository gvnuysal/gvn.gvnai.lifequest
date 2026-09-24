using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Progression;

namespace LifeQuest.Domain.Tests;

public sealed class EconomyTests
{
    /// <summary>Analiz Ek B: Weekly / Medium → 180 Life XP + 120 Culture XP + 40 Explorer XP.</summary>
    [Fact]
    public void Weekly_medium_familiar_quest_matches_paper_example()
    {
        var reward = RewardCalculator.Calculate(QuestType.Weekly, Difficulty.Medium, hasSecondaryCategory: true,
            RewardCalculator.FamiliarMultiplier);

        Assert.Equal(180, reward.LifeXp);
        Assert.Equal(120, reward.PrimaryCategoryXp);
        Assert.Equal(40, reward.SecondaryCategoryXp);
    }

    [Theory]
    [InlineData(false, false, 1.25)]
    [InlineData(true, false, 1.10)]
    [InlineData(true, true, 1.00)]
    public void Novelty_multiplier_rewards_new_experiences(bool categoryDone, bool templateDone, decimal expected)
        => Assert.Equal(expected, RewardCalculator.NoveltyMultiplier(categoryDone, templateDone));

    [Fact]
    public void Daily_quest_without_secondary_category_gets_no_secondary_xp()
    {
        var reward = RewardCalculator.Calculate(QuestType.Daily, Difficulty.Easy, false, 1.25m);

        Assert.Equal(38, reward.LifeXp);
        Assert.Equal(25, reward.PrimaryCategoryXp);
        Assert.Equal(0, reward.SecondaryCategoryXp);
    }

    [Fact]
    public void Xp_is_clamped_to_economy_limits()
    {
        var reward = RewardCalculator.Calculate(QuestType.Epic, Difficulty.Heroic, true, 10m);
        Assert.Equal(RewardCalculator.MaxLifeXp, reward.LifeXp);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(299, 2)]
    [InlineData(300, 3)]
    [InlineData(1000, 5)]
    [InlineData(4500, 10)]
    public void Life_level_curve(int xp, int expectedLevel)
        => Assert.Equal(expectedLevel, LevelCurve.LevelForXp(xp, LevelCurve.LifeBase));

    [Fact]
    public void Level_curve_is_capped()
        => Assert.Equal(LevelCurve.MaxLevel, LevelCurve.LevelForXp(int.MaxValue / 2, LevelCurve.LifeBase));
}
