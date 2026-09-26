namespace LifeQuest.Domain.Common;

/// <summary>
/// Life Profile alanları. Seviyeler bir kişilik testi değil, yalnızca tamamlanan deneyimlerin yansımasıdır.
/// </summary>
public enum LifeCategory
{
    Explorer = 1,
    Culture = 2,
    Learning = 3,
    Social = 4,
    Fitness = 5,
    Creativity = 6
}

public static class LifeCategories
{
    public static readonly IReadOnlyList<LifeCategory> All = Enum.GetValues<LifeCategory>();

    /// <summary>O anki dilde ad.</summary>
    public static string DisplayName(this LifeCategory category) => category.LocalizedName().Current;

    public static Localization.LocalizedText LocalizedName(this LifeCategory category) => category switch
    {
        LifeCategory.Explorer => new("Keşif", "Exploration"),
        LifeCategory.Culture => new("Kültür", "Culture"),
        LifeCategory.Learning => new("Öğrenme", "Learning"),
        LifeCategory.Social => new("Sosyal", "Social"),
        LifeCategory.Fitness => new("Hareket", "Movement"),
        LifeCategory.Creativity => new("Yaratıcılık", "Creativity"),
        _ => Localization.LocalizedText.Same(category.ToString())
    };
}
