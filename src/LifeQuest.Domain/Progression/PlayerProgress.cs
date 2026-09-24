using Gvn.GvnFramework.Domain.Aggregates;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Domain.Progression;

/// <summary>
/// Kullanıcının Life XP, kategori XP, seviye ve başarımları. Profil aggregate'inden ayrıdır: quest
/// tamamlama ile tercih güncellemesi aynı satır için yarışmaz.
/// </summary>
public sealed class PlayerProgress : AggregateRoot
{
    private readonly List<CategoryProgress> _categories = [];
    private readonly List<UnlockedAchievement> _achievements = [];

    public Guid UserId { get; private set; }
    public int LifeXp { get; private set; }
    public int LifeLevel { get; private set; } = 1;
    public int TotalCompleted { get; private set; }
    public int FeedbackCount { get; private set; }

    public IReadOnlyCollection<CategoryProgress> Categories => _categories.AsReadOnly();
    public IReadOnlyCollection<UnlockedAchievement> Achievements => _achievements.AsReadOnly();

    private PlayerProgress() { }

    public static PlayerProgress CreateFor(Guid userId)
    {
        var progress = new PlayerProgress { UserId = userId };
        foreach (var category in LifeCategories.All)
            progress._categories.Add(new CategoryProgress(progress.Id, category));

        return progress;
    }

    public CategoryProgress CategoryOf(LifeCategory category)
        => _categories.First(c => c.Category == category);

    public bool HasAchievement(string code) => _achievements.Any(a => a.Code == code);

    /// <summary>
    /// Tamamlanan quest'in snapshot ödülünü uygular ve deftere yazılacak kaydı döner.
    /// Çağıran taraf yalnızca <see cref="UserQuest.Complete"/> <c>true</c> döndüğünde çağırmalıdır;
    /// defterdeki benzersiz indeks ikinci bir güvenlik katmanıdır.
    /// </summary>
    public RewardOutcome ApplyQuestReward(UserQuest quest, DateTime nowUtc)
    {
        var reward = quest.Reward;

        LifeXp += reward.LifeXp;
        TotalCompleted++;

        var previousLevel = LifeLevel;
        LifeLevel = LevelCurve.LevelForXp(LifeXp, LevelCurve.LifeBase);
        if (LifeLevel > previousLevel)
            AddDomainEvent(new LifeLevelUpEvent(UserId, LifeLevel));

        var primary = CategoryOf(quest.Category);
        if (primary.AddXp(reward.PrimaryCategoryXp, countsAsCompletion: true))
            AddDomainEvent(new CategoryLevelUpEvent(UserId, primary.Category, primary.Level));

        if (quest.SecondaryCategory is { } secondaryCategory && reward.SecondaryCategoryXp > 0)
        {
            var secondary = CategoryOf(secondaryCategory);
            if (secondary.AddXp(reward.SecondaryCategoryXp, countsAsCompletion: false))
                AddDomainEvent(new CategoryLevelUpEvent(UserId, secondary.Category, secondary.Level));
        }

        var transaction = new XpTransaction(
            UserId, XpSourceType.Quest, quest.Id, quest.Title,
            reward.LifeXp, quest.Category, reward.PrimaryCategoryXp,
            quest.SecondaryCategory, reward.SecondaryCategoryXp, nowUtc);

        return new RewardOutcome(transaction, previousLevel, LifeLevel, EvaluateAchievements(nowUtc));
    }

    public IReadOnlyList<AchievementDefinition> RegisterFeedback(DateTime nowUtc)
    {
        FeedbackCount++;
        return EvaluateAchievements(nowUtc);
    }

    private IReadOnlyList<AchievementDefinition> EvaluateAchievements(DateTime nowUtc)
    {
        var unlocked = AchievementCatalog.All
            .Where(a => !HasAchievement(a.Code) && a.IsSatisfied(this))
            .ToList();

        foreach (var achievement in unlocked)
        {
            _achievements.Add(new UnlockedAchievement(Id, achievement.Code, nowUtc));
            AddDomainEvent(new AchievementUnlockedEvent(UserId, achievement.Code));
        }

        return unlocked;
    }
}

public sealed record RewardOutcome(
    XpTransaction Transaction,
    int PreviousLifeLevel,
    int NewLifeLevel,
    IReadOnlyList<AchievementDefinition> NewAchievements)
{
    public bool LeveledUp => NewLifeLevel > PreviousLifeLevel;
}
