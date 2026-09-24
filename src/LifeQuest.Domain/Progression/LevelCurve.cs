namespace LifeQuest.Domain.Progression;

/// <summary>
/// Seviye eğrisi: L seviyesine ulaşmak için gereken toplam XP = base × (L-1) × L / 2.
/// Life XP için base=100 (L2=100, L3=300, L5=1000, L10=4500), kategori XP için base=60.
/// </summary>
public static class LevelCurve
{
    public const int LifeBase = 100;
    public const int CategoryBase = 60;
    public const int MaxLevel = 100;

    public static int CumulativeXpForLevel(int level, int baseXp)
    {
        level = Math.Clamp(level, 1, MaxLevel);
        return baseXp * (level - 1) * level / 2;
    }

    public static int LevelForXp(int xp, int baseXp)
    {
        var level = 1;
        while (level < MaxLevel && xp >= CumulativeXpForLevel(level + 1, baseXp))
            level++;

        return level;
    }
}
