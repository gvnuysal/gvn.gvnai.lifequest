using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Simulation;

/// <summary>
/// Gizli (gerçek) ilgi haritası olan sentetik kullanıcı. Onboarding'de bu haritanın yalnızca bir kısmını
/// beyan eder (<see cref="Declared"/>); <see cref="Hidden"/> ilgiler kullanıcının farkında olmadığı ama
/// sevdiği alanlardır, sistemin bunları keşfedip keşfedemediği ölçülür.
/// </summary>
internal sealed record Persona(
    string Key,
    string Name,
    Dictionary<string, double> Affinity,
    string[] Declared,
    string[] Hidden,
    LifeCategory[] Goals,
    CostBand Budget,
    CostBand Comfort,
    int WeeklyMinutes,
    int SessionMinutes,
    DiscoveryRadius Radius,
    bool HasCity,
    PhysicalEffort Ability)
{
    public const double BaselineAffinity = 0.15;

    /// <summary>Quest'in gerçek çekiciliği: etiketlerinden en sevileni.</summary>
    public double TrueAffinity(IEnumerable<string> interestCodes)
        => interestCodes.Select(c => Affinity.GetValueOrDefault(c, BaselineAffinity)).DefaultIfEmpty(BaselineAffinity).Max();

    public static readonly IReadOnlyList<Persona> All =
    [
        new("coffee", "Kahve ve kafe tutkunu",
            new() { ["coffee"] = 0.95, ["cafe-culture"] = 0.9, ["street-food"] = 0.8, ["architecture"] = 0.7, ["photography"] = 0.65, ["neighborhoods"] = 0.6, ["local-markets"] = 0.5 },
            ["coffee", "cafe-culture", "street-food"], ["architecture", "photography"],
            [LifeCategory.Explorer], CostBand.Medium, CostBand.Medium, 300, 120, DiscoveryRadius.Explore, true, PhysicalEffort.Vigorous),
        new("culture", "Kültür meraklısı",
            new() { ["cinema"] = 0.9, ["theatre"] = 0.85, ["museums"] = 0.8, ["art"] = 0.8, ["history"] = 0.75, ["writing"] = 0.65, ["architecture"] = 0.6 },
            ["cinema", "museums", "art"], ["writing", "architecture"],
            [LifeCategory.Culture], CostBand.Medium, CostBand.Medium, 300, 180, DiscoveryRadius.Chill, true, PhysicalEffort.Vigorous),
        new("student", "Düşük bütçeli öğrenci",
            new() { ["reading"] = 0.85, ["languages"] = 0.8, ["podcasts"] = 0.75, ["science"] = 0.7, ["board-games"] = 0.7, ["friends"] = 0.7, ["astronomy"] = 0.6 },
            ["reading", "languages", "friends"], ["science", "astronomy"],
            [LifeCategory.Learning, LifeCategory.Social], CostBand.Free, CostBand.Low, 600, 150, DiscoveryRadius.Explore, true, PhysicalEffort.Vigorous),
        new("busy", "Az vakitli çalışan",
            new() { ["podcasts"] = 0.8, ["meditation"] = 0.8, ["yoga"] = 0.75, ["cooking"] = 0.7, ["reading"] = 0.6, ["walking"] = 0.55 },
            ["podcasts", "yoga", "cooking"], ["meditation"],
            [LifeCategory.Fitness, LifeCategory.Learning], CostBand.Low, CostBand.Low, 120, 30, DiscoveryRadius.Chill, true, PhysicalEffort.Vigorous),
        new("mobility", "Hareket kısıtı olan kullanıcı",
            new() { ["crafts"] = 0.85, ["drawing"] = 0.8, ["art"] = 0.8, ["writing"] = 0.75, ["music-making"] = 0.7, ["history"] = 0.6, ["cinema"] = 0.6 },
            ["crafts", "drawing", "art"], ["writing", "music-making"],
            [LifeCategory.Creativity, LifeCategory.Culture], CostBand.Low, CostBand.Low, 300, 120, DiscoveryRadius.Explore, true, PhysicalEffort.Light),
        new("social", "Sosyal kelebek",
            new() { ["friends"] = 0.95, ["board-games"] = 0.85, ["community-events"] = 0.8, ["live-music"] = 0.75, ["dance"] = 0.7, ["volunteering"] = 0.7, ["cooking"] = 0.6 },
            ["friends", "board-games", "live-music"], ["dance", "volunteering"],
            [LifeCategory.Social], CostBand.Medium, CostBand.Medium, 600, 180, DiscoveryRadius.Explore, true, PhysicalEffort.Vigorous),
        new("nocity", "Şehir bilgisi vermeyen doğa sever",
            new() { ["walking"] = 0.85, ["nature"] = 0.8, ["photography"] = 0.75, ["gardening"] = 0.7, ["astronomy"] = 0.7, ["yoga"] = 0.5 },
            ["walking", "photography", "nature"], ["gardening", "astronomy"],
            [LifeCategory.Explorer, LifeCategory.Fitness], CostBand.Free, CostBand.Low, 300, 90, DiscoveryRadius.Explore, false, PhysicalEffort.Vigorous),
        new("surprise", "Şaşırt Beni kaşifi",
            new() { ["photography"] = 0.8, ["cooking"] = 0.8, ["street-food"] = 0.7, ["cycling"] = 0.7, ["museums"] = 0.6, ["music-making"] = 0.6, ["dance"] = 0.6, ["science"] = 0.55 },
            ["photography", "cooking"], ["cycling", "dance"],
            [], CostBand.Medium, CostBand.Medium, 600, 180, DiscoveryRadius.SurpriseMe, true, PhysicalEffort.Vigorous)
    ];
}
