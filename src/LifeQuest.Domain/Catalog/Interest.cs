using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Domain.Entities;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Domain.Catalog;

/// <summary>Taste Graph düğümü (ör. Kahve, Mimari, Sokak Fotoğrafçılığı).</summary>
public sealed class Interest : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public LifeCategory Category { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Interest() { }

    public static Interest Create(string code, string name, LifeCategory category)
        => new()
        {
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)),
            Category = category
        };
}

public enum InterestRelationType
{
    /// <summary>Komşu ilgi alanı: birinden diğerine geçiş doğal.</summary>
    Adjacent = 1,

    /// <summary>Daha spesifik alt alan (Fotoğrafçılık → Sokak Fotoğrafçılığı).</summary>
    Specialization = 2
}

public enum RelationSource
{
    Editorial = 1,
    UserBehavior = 2,
    AiSuggested = 3
}

/// <summary>
/// Taste Graph kenarı. MVP'de PostgreSQL tablosu yeterli; kaynak (editoryal / davranış / AI) ayrı tutulur ki
/// AI önerili kenarlar editoryal onay olmadan düşük güvenle kullanılabilsin.
/// </summary>
public sealed class InterestRelation : Entity
{
    public Guid FromInterestId { get; private set; }
    public Guid ToInterestId { get; private set; }
    public InterestRelationType RelationType { get; private set; }
    public double Weight { get; private set; }
    public double Confidence { get; private set; }
    public RelationSource Source { get; private set; }

    private InterestRelation() { }

    public static InterestRelation Create(
        Guid fromInterestId, Guid toInterestId, InterestRelationType type,
        double weight, double confidence, RelationSource source)
    {
        Guard.True(fromInterestId != toInterestId, Text.Of("Bir ilgi alanı kendisiyle ilişkilendirilemez.", "An interest can't be related to itself."));

        return new InterestRelation
        {
            FromInterestId = fromInterestId,
            ToInterestId = toInterestId,
            RelationType = type,
            Weight = Guard.InRange(weight, 0d, 1d, nameof(weight)),
            Confidence = Guard.InRange(confidence, 0d, 1d, nameof(confidence)),
            Source = source
        };
    }
}
