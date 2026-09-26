using Gvn.GvnFramework.Domain.Entities;
using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Progression;

public sealed class CategoryProgress : Entity
{
    public Guid PlayerProgressId { get; private set; }
    public LifeCategory Category { get; private set; }
    public int Xp { get; private set; }
    public int Level { get; private set; } = 1;
    public int CompletedCount { get; private set; }

    private CategoryProgress() { }

    internal CategoryProgress(Guid playerProgressId, LifeCategory category)
    {
        PlayerProgressId = playerProgressId;
        Category = category;
    }

    /// <returns>Seviye atladıysa <c>true</c>.</returns>
    internal bool AddXp(int xp, bool countsAsCompletion)
    {
        Xp += xp;
        if (countsAsCompletion)
            CompletedCount++;

        var previous = Level;
        Level = LevelCurve.LevelForXp(Xp, LevelCurve.CategoryBase);
        return Level > previous;
    }
}

public sealed class UnlockedAchievement : Entity
{
    public Guid PlayerProgressId { get; private set; }
    public string Code { get; private set; } = default!;
    public DateTime UnlockedAt { get; private set; }

    private UnlockedAchievement() { }

    internal UnlockedAchievement(Guid playerProgressId, string code, DateTime nowUtc)
    {
        PlayerProgressId = playerProgressId;
        Code = code;
        UnlockedAt = nowUtc;
    }
}

public enum XpSourceType
{
    Quest = 1,
    /// <summary>Quest Party "birlikte" bonusu; kaynak üyenin kendi görevidir (görev başına bir kez).</summary>
    PartyBonus = 2
}

/// <summary>
/// Append-only XP defteri. (SourceType, SourceId) benzersizdir; eşzamanlı iki "complete" isteği ya da
/// iki kez çalışan bir job aynı quest için çift XP yazamaz. Ekonominin denetlenebilir kaydıdır.
/// </summary>
public sealed class XpTransaction : Entity
{
    public Guid UserId { get; private set; }
    public XpSourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public string Description { get; private set; } = default!;
    public int LifeXp { get; private set; }
    public LifeCategory PrimaryCategory { get; private set; }
    public int PrimaryCategoryXp { get; private set; }
    public LifeCategory? SecondaryCategory { get; private set; }
    public int SecondaryCategoryXp { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private XpTransaction() { }

    internal XpTransaction(
        Guid userId, XpSourceType sourceType, Guid sourceId, string description,
        int lifeXp, LifeCategory primaryCategory, int primaryCategoryXp,
        LifeCategory? secondaryCategory, int secondaryCategoryXp, DateTime nowUtc)
    {
        UserId = userId;
        SourceType = sourceType;
        SourceId = sourceId;
        Description = description;
        LifeXp = lifeXp;
        PrimaryCategory = primaryCategory;
        PrimaryCategoryXp = primaryCategoryXp;
        SecondaryCategory = secondaryCategory;
        SecondaryCategoryXp = secondaryCategoryXp;
        CreatedAt = nowUtc;
    }
}
