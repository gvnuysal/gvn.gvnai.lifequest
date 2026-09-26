using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Application.Common;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Community;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Admin.Catalog;

/// <summary>
/// Admin formundan gelen template tanımı. Gün dilimleri istemcide liste olarak taşınır ve bayrak enum'una çevrilir.
/// </summary>
public sealed record TemplateInput(
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
    IReadOnlyList<DayPart> DayParts,
    bool RequiresCity,
    bool IsOutdoor,
    int CooldownDays,
    double RiskScore,
    IReadOnlyList<Guid> InterestIds,
    PhysicalEffort Effort,
    bool IsStarter)
{
    public QuestTemplateSpec ToSpec() => new(
        Code.Trim(), Title.Trim(), Description.Trim(), Type, Difficulty, Category, SecondaryCategory, MinMinutes, MaxMinutes,
        Cost, DayParts.Aggregate(DayPart.None, (all, p) => all | p), RequiresCity, IsOutdoor, CooldownDays,
        Math.Round(RiskScore, 2), InterestIds.Distinct().ToList(), Effort, IsStarter);
}

/// <summary>
/// Domain guard'larının fırlatacağı durumları 400'e çevirir. Editoryal kurallar (<see cref="CatalogSafetyRules"/>)
/// burada değil: onlara uymayan template kaydedilir ama <see cref="SafetyLevel.NeedsReview"/> olur.
/// </summary>
public sealed class TemplateInputValidator : AbstractValidator<TemplateInput>
{
    public TemplateInputValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Matches("^[a-z0-9-]{3,64}$")
            .WithMessage(_ => Text.Of("Kod 3-64 karakter; küçük harf, rakam ve tire içermelidir.", "The code must be 3-64 characters of lowercase letters, digits and hyphens."));
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.SecondaryCategory).IsInEnum()
            .NotEqual(x => x.Category).WithMessage(_ => Text.Of("İkincil kategori birincil kategoriyle aynı olamaz.", "The secondary category can't be the same as the primary."));
        RuleFor(x => x.MinMinutes).InclusiveBetween(1, 10080);
        RuleFor(x => x.MaxMinutes).InclusiveBetween(1, 10080)
            .GreaterThanOrEqualTo(x => x.MinMinutes).WithMessage(_ => Text.Of("En uzun süre en kısa süreden küçük olamaz.", "The longest duration can't be shorter than the shortest."));
        RuleFor(x => x.Cost).IsInEnum();
        RuleFor(x => x.DayParts).NotEmpty().WithMessage(_ => Text.Of("En az bir gün dilimi seçilmelidir.", "Pick at least one part of the day."));
        RuleForEach(x => x.DayParts).Must(p => p is DayPart.Morning or DayPart.Afternoon or DayPart.Evening or DayPart.Night)
            .WithMessage(_ => Text.Of("Geçersiz gün dilimi.", "Invalid part of the day."));
        RuleFor(x => x.CooldownDays).InclusiveBetween(0, 365);
        RuleFor(x => x.RiskScore).InclusiveBetween(0, 1);
        RuleFor(x => x.InterestIds).NotEmpty().WithMessage(_ => Text.Of("En az bir ilgi alanı seçilmelidir.", "Pick at least one interest."));
        RuleFor(x => x.Effort).IsInEnum();
    }
}

public sealed record AdminTemplateDto(
    Guid Id,
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
    IReadOnlyList<DayPart> DayParts,
    bool RequiresCity,
    bool IsOutdoor,
    int CooldownDays,
    double RiskScore,
    IReadOnlyList<Guid> InterestIds,
    PhysicalEffort Effort,
    bool IsStarter,
    SafetyLevel Safety,
    bool IsActive,
    EditorialSource Source,
    int Version,
    IReadOnlyList<string> Violations);

public sealed record AdminTemplateListItem(
    Guid Id,
    string Code,
    string Title,
    LifeCategory Category,
    QuestType Type,
    CostBand Cost,
    SafetyLevel Safety,
    bool IsActive,
    EditorialSource Source,
    int Version,
    int ViolationCount);

internal static class AdminTemplateMapping
{
    private static readonly DayPart[] Parts = [DayPart.Morning, DayPart.Afternoon, DayPart.Evening, DayPart.Night];

    public static IReadOnlyList<DayPart> Split(DayPart parts) => Parts.Where(p => parts.HasFlag(p)).ToList();

    public static AdminTemplateDto ToAdminDto(this QuestTemplate t) => new(
        t.Id, t.Code, t.Title, t.Description, t.Type, t.Difficulty, t.Category, t.SecondaryCategory, t.MinMinutes, t.MaxMinutes,
        t.Cost, Split(t.DayParts), t.RequiresCity, t.IsOutdoor, t.CooldownDays, t.RiskScore, t.InterestIds, t.Effort,
        t.IsStarter, t.Safety, t.IsActive, t.Source, t.Version, CatalogSafetyRules.ValidateTemplate(t.ToSpec()));

    public static AdminTemplateListItem ToListItem(this QuestTemplate t) => new(
        t.Id, t.Code, t.Title, t.Category, t.Type, t.Cost, t.Safety, t.IsActive, t.Source, t.Version,
        CatalogSafetyRules.ValidateTemplate(t.ToSpec()).Count);

    /// <summary>Blocked admin kararıdır ve korunur; diğer durumlarda kurallar Safe / NeedsReview'u belirler.</summary>
    public static SafetyLevel SafetyFor(IReadOnlyList<string> violations, SafetyLevel current)
        => current == SafetyLevel.Blocked ? SafetyLevel.Blocked
            : violations.Count == 0 ? SafetyLevel.Safe : SafetyLevel.NeedsReview;
}

/// <summary>Formdaki ilgi alanlarının katalogda var olduğunu doğrular.</summary>
internal static class InterestCheck
{
    public static async Task<Error?> FindUnknownAsync(IQuestCatalog catalog, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var known = (await catalog.GetInterestsAsync(ct)).Select(i => i.Id).ToHashSet();
        var unknown = ids.Where(id => !known.Contains(id)).ToList();
        return unknown.Count == 0 ? null : AdminErrors.UnknownInterests(unknown);
    }
}

// ── Sorgular ──────────────────────────────────────────────────────────────────

public sealed record SearchTemplatesQuery(
    string? Text, LifeCategory? Category, QuestType? Type, SafetyLevel? Safety, bool? IsActive,
    int PageNumber = 1, int PageSize = 20) : IQuery<PagedResult<AdminTemplateListItem>>;

public sealed class SearchTemplatesQueryValidator : AbstractValidator<SearchTemplatesQuery>
{
    public SearchTemplatesQueryValidator()
    {
        RuleFor(x => x.Text).MaximumLength(100);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Safety).IsInEnum();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
    }
}

internal sealed class SearchTemplatesQueryHandler(IQuestTemplateRepository templates)
    : IQueryHandler<SearchTemplatesQuery, PagedResult<AdminTemplateListItem>>
{
    public async Task<Result<PagedResult<AdminTemplateListItem>>> Handle(SearchTemplatesQuery query, CancellationToken cancellationToken)
    {
        var paging = new PagedRequest { PageNumber = query.PageNumber, PageSize = query.PageSize };
        var search = new TemplateSearch(
            string.IsNullOrWhiteSpace(query.Text) ? null : query.Text.Trim(), query.Category, query.Type, query.Safety, query.IsActive);
        var (items, total) = await templates.SearchAsync(search, paging.Skip, paging.PageSize, cancellationToken);

        return Result<PagedResult<AdminTemplateListItem>>.Ok(new PagedResult<AdminTemplateListItem>(
            items.Select(t => t.ToListItem()), total, paging.PageNumber, paging.PageSize));
    }
}

public sealed record GetTemplateQuery(Guid TemplateId) : IQuery<AdminTemplateDto>;

internal sealed class GetTemplateQueryHandler(IQuestTemplateRepository templates) : IQueryHandler<GetTemplateQuery, AdminTemplateDto>
{
    public async Task<Result<AdminTemplateDto>> Handle(GetTemplateQuery query, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(query.TemplateId, cancellationToken);
        return template is null
            ? Result<AdminTemplateDto>.Fail(AdminErrors.TemplateNotFound)
            : Result<AdminTemplateDto>.Ok(template.ToAdminDto());
    }
}

public sealed record TemplateValidationDto(IReadOnlyList<string> Violations, SafetyLevel ResultingSafety);

/// <summary>Kaydetmeden önce editoryal kuralları kontrol eder (formdaki "Kontrol et").</summary>
public sealed record ValidateTemplateQuery(TemplateInput Template) : IQuery<TemplateValidationDto>;

public sealed class ValidateTemplateQueryValidator : AbstractValidator<ValidateTemplateQuery>
{
    public ValidateTemplateQueryValidator() => RuleFor(x => x.Template).NotNull().SetValidator(new TemplateInputValidator());
}

internal sealed class ValidateTemplateQueryHandler : IQueryHandler<ValidateTemplateQuery, TemplateValidationDto>
{
    public Task<Result<TemplateValidationDto>> Handle(ValidateTemplateQuery query, CancellationToken cancellationToken)
    {
        var violations = CatalogSafetyRules.ValidateTemplate(query.Template.ToSpec());
        return Task.FromResult(Result<TemplateValidationDto>.Ok(new TemplateValidationDto(
            violations, AdminTemplateMapping.SafetyFor(violations, SafetyLevel.Safe))));
    }
}

public sealed record CategoryHealthDto(LifeCategory Category, int Templates, int Daily);

public sealed record CatalogHealthDto(
    int Offerable,
    int NeedsReview,
    int Blocked,
    int PendingIdeas,
    double FreeShare,
    double CityIndependentShare,
    IReadOnlyList<CategoryHealthDto> Categories,
    IReadOnlyList<string> Warnings);

/// <summary>Önerilebilir katalogun editoryal dengesi (kategori başına sayı, ücretsiz ve şehirden bağımsız pay).</summary>
public sealed record GetCatalogHealthQuery : IQuery<CatalogHealthDto>;

internal sealed class GetCatalogHealthQueryHandler(
    IQuestTemplateRepository templates, IQuestIdeaRepository ideas, IQuestCatalog catalog)
    : IQueryHandler<GetCatalogHealthQuery, CatalogHealthDto>
{
    public async Task<Result<CatalogHealthDto>> Handle(GetCatalogHealthQuery query, CancellationToken cancellationToken)
    {
        var specs = await templates.GetOfferableSpecsAsync(cancellationToken);
        var interests = (await catalog.GetInterestsAsync(cancellationToken)).ToDictionary(i => i.Id, i => i.Name);
        var total = Math.Max(1, specs.Count);

        return Result<CatalogHealthDto>.Ok(new CatalogHealthDto(
            specs.Count,
            await templates.CountBySafetyAsync(SafetyLevel.NeedsReview, cancellationToken),
            await templates.CountBySafetyAsync(SafetyLevel.Blocked, cancellationToken),
            await ideas.CountByStatusAsync(IdeaStatus.Pending, cancellationToken),
            Math.Round(specs.Count(s => s.Cost == CostBand.Free) / (double)total, 3),
            Math.Round(specs.Count(s => !s.RequiresCity) / (double)total, 3),
            LifeCategories.All
                .Select(c => new CategoryHealthDto(c, specs.Count(s => s.Category == c), specs.Count(s => s.Category == c && s.Type == QuestType.Daily)))
                .ToList(),
            // Kategori dengesi + ilgi alanı derinliği: admin'in (ve topluluk fikirlerinin) nereye içerik gerektiğini gösterir.
            [.. CatalogSafetyRules.ValidateCatalog(specs.ToList()),
             .. CatalogSafetyRules.ValidateInterestCoverage(specs.ToList(), interests)]));
    }
}

// ── Komutlar ──────────────────────────────────────────────────────────────────

/// <param name="SourceIdeaId">Template bir topluluk fikrinden oluşturuluyorsa fikir; oluşturulunca kabul edilmiş sayılır.</param>
public sealed record CreateTemplateCommand(TemplateInput Template, Guid? SourceIdeaId = null) : ICommand<AdminTemplateDto>;

public sealed class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator() => RuleFor(x => x.Template).NotNull().SetValidator(new TemplateInputValidator());
}

internal sealed class CreateTemplateCommandHandler(
    IQuestTemplateRepository templates,
    IQuestIdeaRepository ideas,
    IQuestCatalog catalog,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateTemplateCommand, AdminTemplateDto>
{
    public async Task<Result<AdminTemplateDto>> Handle(CreateTemplateCommand command, CancellationToken cancellationToken)
    {
        var spec = command.Template.ToSpec();
        if (await templates.CodeExistsAsync(spec.Code, cancellationToken))
            return Result<AdminTemplateDto>.Fail(AdminErrors.TemplateCodeTaken);
        if (await InterestCheck.FindUnknownAsync(catalog, spec.InterestIds, cancellationToken) is { } unknown)
            return Result<AdminTemplateDto>.Fail(unknown);

        QuestIdea? idea = null;
        if (command.SourceIdeaId is { } ideaId)
        {
            idea = await ideas.GetByIdAsync(ideaId, cancellationToken);
            if (idea is null)
                return Result<AdminTemplateDto>.Fail(IdeaErrors.NotFound);
            if (idea.Status != IdeaStatus.Pending)
                return Result<AdminTemplateDto>.Fail(IdeaErrors.AlreadyReviewed);
        }

        var violations = CatalogSafetyRules.ValidateTemplate(spec);
        var template = QuestTemplate.Create(spec, AdminTemplateMapping.SafetyFor(violations, SafetyLevel.Safe), EditorialSource.Admin);
        await templates.AddAsync(template, cancellationToken);
        await audit.RecordAsync(AdminAction.TemplateCreated, AdminTargetType.QuestTemplate, template.Id, template.Code,
            null, new { template.Safety, violations, sourceIdeaId = command.SourceIdeaId }, cancellationToken);

        if (idea is not null)
        {
            var actor = await audit.GetActorAsync(cancellationToken);
            idea.Accept(template.Id, actor.Email, Text.Of("Fikrin kataloğa eklendi, teşekkürler!", "Your idea was added to the catalog, thank you!"), clock.GetUtcNow().UtcDateTime);
            await audit.RecordAsync(AdminAction.IdeaAccepted, AdminTargetType.QuestIdea, idea.Id, idea.Title, null,
                new { templateId = template.Id, template.Code }, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await catalog.InvalidateAsync(cancellationToken);

        return Result<AdminTemplateDto>.Ok(template.ToAdminDto());
    }
}

/// <param name="Version">İstemcinin düzenlediği sürüm; arada başka bir admin kaydettiyse 409.</param>
public sealed record UpdateTemplateCommand(Guid TemplateId, int Version, TemplateInput Template) : ICommand<AdminTemplateDto>;

public sealed class UpdateTemplateCommandValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateCommandValidator() => RuleFor(x => x.Template).NotNull().SetValidator(new TemplateInputValidator());
}

internal sealed class UpdateTemplateCommandHandler(
    IQuestTemplateRepository templates,
    IQuestCatalog catalog,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTemplateCommand, AdminTemplateDto>
{
    public async Task<Result<AdminTemplateDto>> Handle(UpdateTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result<AdminTemplateDto>.Fail(AdminErrors.TemplateNotFound);
        if (template.Version != command.Version)
            return Result<AdminTemplateDto>.Fail(AdminErrors.StaleVersion);

        var spec = command.Template.ToSpec() with { Code = template.Code };
        if (await InterestCheck.FindUnknownAsync(catalog, spec.InterestIds, cancellationToken) is { } unknown)
            return Result<AdminTemplateDto>.Fail(unknown);

        var before = template.ToSpec();
        var violations = CatalogSafetyRules.ValidateTemplate(spec);
        var changed = template.ApplyAdminEdit(spec);
        var safety = AdminTemplateMapping.SafetyFor(violations, template.Safety);
        var safetyChanged = safety != template.Safety;
        template.DecideSafety(safety);

        if (changed || safetyChanged)
        {
            await audit.RecordAsync(AdminAction.TemplateUpdated, AdminTargetType.QuestTemplate, template.Id, template.Code,
                null, new { before, after = spec, template.Safety, violations }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await catalog.InvalidateAsync(cancellationToken);
        return Result<AdminTemplateDto>.Ok(template.ToAdminDto());
    }
}

/// <summary>Admin'in güvenlik kararı: onayla (Safe), incelemeye al (NeedsReview) veya engelle (Blocked).</summary>
public sealed record SetTemplateSafetyCommand(Guid TemplateId, SafetyLevel Safety, string? Note) : ICommand<AdminTemplateDto>;

public sealed class SetTemplateSafetyCommandValidator : AbstractValidator<SetTemplateSafetyCommand>
{
    public SetTemplateSafetyCommandValidator()
    {
        RuleFor(x => x.Safety).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal sealed class SetTemplateSafetyCommandHandler(
    IQuestTemplateRepository templates,
    IQuestCatalog catalog,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork) : ICommandHandler<SetTemplateSafetyCommand, AdminTemplateDto>
{
    public async Task<Result<AdminTemplateDto>> Handle(SetTemplateSafetyCommand command, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result<AdminTemplateDto>.Fail(AdminErrors.TemplateNotFound);

        var violations = CatalogSafetyRules.ValidateTemplate(template.ToSpec());
        var needsNote = command.Safety == SafetyLevel.Blocked || (command.Safety == SafetyLevel.Safe && violations.Count > 0);
        if (needsNote && string.IsNullOrWhiteSpace(command.Note))
            return Result<AdminTemplateDto>.Fail(AdminErrors.ApprovalNeedsNote);

        if (template.Safety != command.Safety)
        {
            var before = template.Safety;
            template.DecideSafety(command.Safety);
            await audit.RecordAsync(AdminAction.TemplateSafetyChanged, AdminTargetType.QuestTemplate, template.Id, template.Code,
                command.Note, new { before, after = command.Safety, violations }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await catalog.InvalidateAsync(cancellationToken);
        }

        return Result<AdminTemplateDto>.Ok(template.ToAdminDto());
    }
}

public sealed record SetTemplateActiveCommand(Guid TemplateId, bool IsActive) : ICommand<AdminTemplateDto>;

internal sealed class SetTemplateActiveCommandHandler(
    IQuestTemplateRepository templates,
    IQuestCatalog catalog,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork) : ICommandHandler<SetTemplateActiveCommand, AdminTemplateDto>
{
    public async Task<Result<AdminTemplateDto>> Handle(SetTemplateActiveCommand command, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result<AdminTemplateDto>.Fail(AdminErrors.TemplateNotFound);

        var changed = command.IsActive ? template.Activate() : template.Deactivate();
        if (changed)
        {
            await audit.RecordAsync(command.IsActive ? AdminAction.TemplateActivated : AdminAction.TemplateDeactivated,
                AdminTargetType.QuestTemplate, template.Id, template.Code, null, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await catalog.InvalidateAsync(cancellationToken);
        }

        return Result<AdminTemplateDto>.Ok(template.ToAdminDto());
    }
}
