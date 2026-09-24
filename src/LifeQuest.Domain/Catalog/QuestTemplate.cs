using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Domain.Aggregates;
using Gvn.GvnFramework.Domain.Common;
using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Catalog;

/// <summary>
/// Tekrar kullanılabilir quest tanımı. Kullanıcıya verilen quest bunun bir snapshot'ıdır (UserQuest);
/// template sonradan değişse de (Version artar) geçmiş quest'ler bozulmaz.
/// </summary>
public sealed class QuestTemplate : AggregateRoot, ISoftDeletable
{
    public const int DefaultMinimumAge = 18;

    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public QuestType Type { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public LifeCategory Category { get; private set; }
    public LifeCategory? SecondaryCategory { get; private set; }
    public int MinMinutes { get; private set; }
    public int MaxMinutes { get; private set; }
    public CostBand Cost { get; private set; }
    public DayPart DayParts { get; private set; }
    public bool RequiresCity { get; private set; }
    public bool IsOutdoor { get; private set; }
    public int CooldownDays { get; private set; }
    public SafetyLevel Safety { get; private set; }

    /// <summary>0 (risksiz) - 1 arası. Recommendation skorunda ceza olarak kullanılır.</summary>
    public double RiskScore { get; private set; }

    public int MinimumAge { get; private set; } = DefaultMinimumAge;
    public List<Guid> InterestIds { get; private set; } = [];
    public int Version { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    private QuestTemplate() { }

    public static QuestTemplate Create(
        string code, string title, string description,
        QuestType type, Difficulty difficulty,
        LifeCategory category, LifeCategory? secondaryCategory,
        int minMinutes, int maxMinutes, CostBand cost, DayPart dayParts,
        bool requiresCity, bool isOutdoor, int cooldownDays, double riskScore,
        IEnumerable<Guid> interestIds, SafetyLevel safety = SafetyLevel.Safe)
    {
        Guard.True(minMinutes > 0 && minMinutes <= maxMinutes, "Süre aralığı geçersiz.");
        Guard.True(secondaryCategory != category, "İkincil kategori birincil kategoriyle aynı olamaz.");
        Guard.True(dayParts != DayPart.None, "En az bir gün dilimi seçilmelidir.");

        return new QuestTemplate
        {
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)),
            Title = Guard.NotNullOrWhiteSpace(title, nameof(title)),
            Description = Guard.NotNullOrWhiteSpace(description, nameof(description)),
            Type = type,
            Difficulty = difficulty,
            Category = category,
            SecondaryCategory = secondaryCategory,
            MinMinutes = minMinutes,
            MaxMinutes = maxMinutes,
            Cost = cost,
            DayParts = dayParts,
            RequiresCity = requiresCity,
            IsOutdoor = isOutdoor,
            CooldownDays = Guard.InRange(cooldownDays, 0, 365, nameof(cooldownDays)),
            RiskScore = Guard.InRange(riskScore, 0d, 1d, nameof(riskScore)),
            InterestIds = Guard.NotEmpty(interestIds, nameof(interestIds)).Distinct().ToList(),
            Safety = safety
        };
    }

    public bool IsOfferable => IsActive && !IsDeleted && Safety == SafetyLevel.Safe;

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }
}
