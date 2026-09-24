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

    public static string DisplayName(this LifeCategory category) => category switch
    {
        LifeCategory.Explorer => "Keşif",
        LifeCategory.Culture => "Kültür",
        LifeCategory.Learning => "Öğrenme",
        LifeCategory.Social => "Sosyal",
        LifeCategory.Fitness => "Hareket",
        LifeCategory.Creativity => "Yaratıcılık",
        _ => category.ToString()
    };
}
