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
    public PhysicalEffort Effort { get; private set; } = PhysicalEffort.Light;

    /// <summary>Onboarding'deki "bunlardan hangisi sana göre?" kartlarında gösterilir (cold start).</summary>
    public bool IsStarter { get; private set; }
    public int Version { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    private QuestTemplate() { }

    public static QuestTemplate Create(QuestTemplateSpec spec, SafetyLevel safety = SafetyLevel.Safe)
    {
        var template = new QuestTemplate { Code = Guard.NotNullOrWhiteSpace(spec.Code, nameof(spec.Code)), Safety = safety };
        template.Apply(spec);
        return template;
    }

    /// <summary>
    /// Editoryal tanımı uygular. Bir alan değiştiyse <see cref="Version"/> artar; kullanıcılara verilmiş quest'ler
    /// snapshot olduğu için etkilenmez.
    /// </summary>
    /// <returns>Değişiklik olduysa <c>true</c>.</returns>
    public bool ApplyEditorial(QuestTemplateSpec spec)
    {
        Guard.True(spec.Code == Code, "Template kodu değiştirilemez.");
        var sameInterests = spec.InterestIds.Distinct().OrderBy(i => i).SequenceEqual(InterestIds.OrderBy(i => i));
        if (sameInterests && ToSpec() with { InterestIds = spec.InterestIds } == spec)
            return false;

        Apply(spec);
        Version++;
        return true;
    }

    /// <summary>Editoryal güvenlik kontrolünden geçemeyen template önerilmez.</summary>
    public void SetSafety(SafetyLevel safety) => Safety = safety;

    public QuestTemplateSpec ToSpec() => new(
        Code, Title, Description, Type, Difficulty, Category, SecondaryCategory, MinMinutes, MaxMinutes,
        Cost, DayParts, RequiresCity, IsOutdoor, CooldownDays, RiskScore, InterestIds, Effort, IsStarter);

    private void Apply(QuestTemplateSpec spec)
    {
        Guard.True(spec.MinMinutes > 0 && spec.MinMinutes <= spec.MaxMinutes, "Süre aralığı geçersiz.");
        Guard.True(spec.SecondaryCategory != spec.Category, "İkincil kategori birincil kategoriyle aynı olamaz.");
        Guard.True(spec.DayParts != DayPart.None, "En az bir gün dilimi seçilmelidir.");

        Title = Guard.NotNullOrWhiteSpace(spec.Title, nameof(spec.Title));
        Description = Guard.NotNullOrWhiteSpace(spec.Description, nameof(spec.Description));
        Type = spec.Type;
        Difficulty = spec.Difficulty;
        Category = spec.Category;
        SecondaryCategory = spec.SecondaryCategory;
        MinMinutes = spec.MinMinutes;
        MaxMinutes = spec.MaxMinutes;
        Cost = spec.Cost;
        DayParts = spec.DayParts;
        RequiresCity = spec.RequiresCity;
        IsOutdoor = spec.IsOutdoor;
        CooldownDays = Guard.InRange(spec.CooldownDays, 0, 365, nameof(spec.CooldownDays));
        RiskScore = Guard.InRange(spec.RiskScore, 0d, 1d, nameof(spec.RiskScore));
        InterestIds = Guard.NotEmpty(spec.InterestIds, nameof(spec.InterestIds)).Distinct().ToList();
        Effort = spec.Effort;
        IsStarter = spec.IsStarter;
    }

    public bool IsOfferable => IsActive && !IsDeleted && Safety == SafetyLevel.Safe;

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }
}
