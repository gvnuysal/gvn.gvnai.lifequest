using Gvn.GvnFramework.Domain.ValueObjects;

namespace LifeQuest.Domain.Quests;

/// <summary>Quest sunulduğu anda hesaplanıp snapshot'a yazılan ödül. LLM tarafından üretilmez.</summary>
public sealed class QuestReward : ValueObject
{
    public int LifeXp { get; private set; }
    public int PrimaryCategoryXp { get; private set; }
    public int SecondaryCategoryXp { get; private set; }
    public decimal NoveltyMultiplier { get; private set; }

    private QuestReward() { }

    public QuestReward(int lifeXp, int primaryCategoryXp, int secondaryCategoryXp, decimal noveltyMultiplier)
    {
        LifeXp = lifeXp;
        PrimaryCategoryXp = primaryCategoryXp;
        SecondaryCategoryXp = secondaryCategoryXp;
        NoveltyMultiplier = noveltyMultiplier;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LifeXp;
        yield return PrimaryCategoryXp;
        yield return SecondaryCategoryXp;
        yield return NoveltyMultiplier;
    }
}
