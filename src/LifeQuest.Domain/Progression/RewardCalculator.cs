using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Domain.Progression;

/// <summary>
/// XP = BaseXP × DifficultyMultiplier × NoveltyMultiplier. Deterministik backend kuralıdır; LLM üretmez,
/// böylece ekonomi manipüle edilemez ve dengelenebilir. Kategori XP'si Life XP'nin sabit oranlarıdır:
/// birincil kategori 2/3, ikincil kategori 2/9 (Weekly/Medium → 180 Life + 120 birincil + 40 ikincil).
/// </summary>
public static class RewardCalculator
{
    public const int MaxLifeXp = 2000;

    public const decimal NewCategoryMultiplier = 1.25m;
    public const decimal NewTemplateMultiplier = 1.10m;
    public const decimal FamiliarMultiplier = 1.00m;

    public static int BaseXp(QuestType type) => type switch
    {
        QuestType.Daily => 30,
        QuestType.Weekly => 120,
        QuestType.Adventure => 250,
        QuestType.Epic => 500,
        _ => 30
    };

    public static decimal DifficultyMultiplier(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 1.0m,
        Difficulty.Medium => 1.5m,
        Difficulty.Hard => 2.0m,
        Difficulty.Heroic => 3.0m,
        _ => 1.0m
    };

    public static decimal NoveltyMultiplier(bool categoryCompletedBefore, bool templateCompletedBefore)
        => !categoryCompletedBefore ? NewCategoryMultiplier
            : !templateCompletedBefore ? NewTemplateMultiplier
            : FamiliarMultiplier;

    public static QuestReward Calculate(QuestType type, Difficulty difficulty, bool hasSecondaryCategory, decimal noveltyMultiplier)
    {
        var raw = BaseXp(type) * DifficultyMultiplier(difficulty) * noveltyMultiplier;
        var lifeXp = (int)Math.Clamp(Math.Round(raw, MidpointRounding.AwayFromZero), 1, MaxLifeXp);

        var primary = (int)Math.Round(lifeXp * 2m / 3m, MidpointRounding.AwayFromZero);
        var secondary = hasSecondaryCategory
            ? (int)Math.Round(lifeXp * 2m / 9m, MidpointRounding.AwayFromZero)
            : 0;

        return new QuestReward(lifeXp, primary, secondary, noveltyMultiplier);
    }
}
