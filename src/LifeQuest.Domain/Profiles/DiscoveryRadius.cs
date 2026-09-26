namespace LifeQuest.Domain.Profiles;

/// <summary>Keşif yoğunluğu kullanıcının kontrolündedir; recommendation engine'deki novelty ağırlığını belirler.</summary>
public enum DiscoveryRadius
{
    /// <summary>Çoğunlukla tanıdık alanlar.</summary>
    Chill = 1,

    /// <summary>Tanıdık ve yeni arasında denge.</summary>
    Explore = 2,

    /// <summary>Daha yüksek yenilik.</summary>
    SurpriseMe = 3
}

public static class DiscoveryRadiusExtensions
{
    public static string DisplayName(this DiscoveryRadius radius) => radius.LocalizedName().Current;

    public static Localization.LocalizedText LocalizedName(this DiscoveryRadius radius) => radius switch
    {
        DiscoveryRadius.Chill => new("Sakin", "Chill"),
        DiscoveryRadius.Explore => new("Dengeli", "Balanced"),
        DiscoveryRadius.SurpriseMe => new("Şaşırt Beni", "Surprise Me"),
        _ => Localization.LocalizedText.Same(radius.ToString())
    };
}

public enum InterestSource
{
    /// <summary>Kullanıcının kendi seçtiği.</summary>
    Explicit = 1,

    /// <summary>Geri bildirimlerden öğrenilen.</summary>
    Learned = 2
}
