namespace LifeQuest.Domain.Catalog;

public enum QuestType
{
    /// <summary>5-20 dakikalık küçük görev.</summary>
    Daily = 1,

    /// <summary>1-3 saatlik deneyim.</summary>
    Weekly = 2,

    /// <summary>Birden fazla adımdan oluşan keşif.</summary>
    Adventure = 3,

    /// <summary>Seyrek, anlamlı ve büyük deneyim.</summary>
    Epic = 4
}

public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Heroic = 4
}

public enum SafetyLevel
{
    /// <summary>Yaş ve risk filtresini geçmiş; kullanıcıya önerilebilir.</summary>
    Safe = 1,

    /// <summary>Editoryal inceleme bekliyor; önerilmez.</summary>
    NeedsReview = 2,

    /// <summary>Katalog seviyesinde engellenmiş.</summary>
    Blocked = 3
}

/// <summary>
/// Fiziksel efor. Güvenliğin tek boyutlu olmadığını (risk, efor, açık hava, saat) ve erişilebilirliği
/// modellemek için kullanılır: kullanıcı <c>MaxPhysicalEffort</c> ile üst sınır belirleyebilir.
/// </summary>
public enum PhysicalEffort
{
    None = 0,
    Light = 1,
    Moderate = 2,
    Vigorous = 3
}
