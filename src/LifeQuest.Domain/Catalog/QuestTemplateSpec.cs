using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Catalog;

/// <summary>
/// Template'in editoryal tanımı. Seed ve (ileride) admin akışı template'leri bu tanımla oluşturur ve günceller;
/// <see cref="CatalogSafetyRules"/> aynı tanımı doğrular.
/// </summary>
public sealed record QuestTemplateSpec(
    string Code,
    string Title,
    string Description,
    QuestType Type,
    Difficulty Difficulty,
    LifeCategory Category,
    LifeCategory? SecondaryCategory,
    int MinMinutes,
    int MaxMinutes,
    CostBand Cost,
    DayPart DayParts,
    bool RequiresCity,
    bool IsOutdoor,
    int CooldownDays,
    double RiskScore,
    IReadOnlyList<Guid> InterestIds,
    PhysicalEffort Effort,
    bool IsStarter);
