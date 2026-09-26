using LifeQuest.Domain.Localization;
using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Admin;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.RealWorld;

namespace LifeQuest.Application.RealWorld;

/// <summary>Kullanıcıya görev detayında gösterilen mekân/etkinlik (yöneticinin notu dahil, kim girdiği hariç).</summary>
public sealed record NearbyPlaceDto(
    Guid Id, LocalPlaceKind Kind, string Name, string? Address, string? Url, string? Note, DateTime? StartsAt, DateTime? EndsAt)
{
    public static NearbyPlaceDto From(LocalPlace p) => new(p.Id, p.Kind, p.Name, p.Address, p.Url, p.Note, p.StartsAt, p.EndsAt);
}

public sealed record PlaceTemplateDto(Guid Id, string Code, string Title);

public sealed record LocalPlaceDto(
    Guid Id,
    LocalPlaceKind Kind,
    string City,
    string Name,
    string? Address,
    string? Url,
    string? Note,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool IsActive,
    bool IsPast,
    IReadOnlyList<PlaceTemplateDto> Templates,
    string CreatedBy,
    DateTime CreatedAt);

internal static class LocalPlaceMapping
{
    public static async Task<IReadOnlyList<LocalPlaceDto>> ToDtosAsync(
        IReadOnlyList<LocalPlace> places, IQuestTemplateRepository templates, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var ids = places.SelectMany(p => p.TemplateIds).Distinct().ToList();
        var byId = (await templates.GetByIdsAsync(ids, cancellationToken)).ToDictionary(t => t.Id);

        return places.Select(p => new LocalPlaceDto(
                p.Id, p.Kind, p.City, p.Name, p.Address, p.Url, p.Note, p.StartsAt, p.EndsAt, p.IsActive,
                p.Kind == LocalPlaceKind.Event && p.EndsAt < nowUtc,
                p.TemplateIds.Where(byId.ContainsKey).Select(id => new PlaceTemplateDto(id, byId[id].Code, LocalizedText.WithFallback(byId[id].Title, byId[id].TitleEn).Current)).ToList(),
                p.CreatedByEmail, p.CreatedAt))
            .ToList();
    }
}

// ── Sorgu ─────────────────────────────────────────────────────────────────────

public sealed record ListLocalPlacesQuery(string? City) : IQuery<IReadOnlyList<LocalPlaceDto>>;

internal sealed class ListLocalPlacesQueryHandler(ILocalPlaceRepository places, IQuestTemplateRepository templates, TimeProvider clock)
    : IQueryHandler<ListLocalPlacesQuery, IReadOnlyList<LocalPlaceDto>>
{
    public async Task<Result<IReadOnlyList<LocalPlaceDto>>> Handle(ListLocalPlacesQuery query, CancellationToken cancellationToken)
    {
        var list = await places.GetAllAsync(string.IsNullOrWhiteSpace(query.City) ? null : CityKey.Normalize(query.City), cancellationToken);
        return Result<IReadOnlyList<LocalPlaceDto>>.Ok(
            await LocalPlaceMapping.ToDtosAsync(list, templates, clock.GetUtcNow().UtcDateTime, cancellationToken));
    }
}

// ── Oluşturma / güncelleme ────────────────────────────────────────────────────

public sealed record SaveLocalPlaceCommand(
    Guid? Id,
    LocalPlaceKind Kind,
    string City,
    string Name,
    string? Address,
    string? Url,
    string? Note,
    DateTime? StartsAt,
    DateTime? EndsAt,
    IReadOnlyList<Guid> TemplateIds,
    bool IsActive = true) : ICommand<LocalPlaceDto>;

public sealed class SaveLocalPlaceCommandValidator : AbstractValidator<SaveLocalPlaceCommand>
{
    public SaveLocalPlaceCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.City).NotEmpty().MaximumLength(CityKey.MaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(500);
        RuleFor(x => x.Note).MaximumLength(300);
        RuleFor(x => x.TemplateIds).NotNull();
    }
}

internal sealed class SaveLocalPlaceCommandHandler(
    ILocalPlaceRepository places,
    IQuestTemplateRepository templates,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<SaveLocalPlaceCommand, LocalPlaceDto>
{
    public async Task<Result<LocalPlaceDto>> Handle(SaveLocalPlaceCommand command, CancellationToken cancellationToken)
    {
        var known = (await templates.GetByIdsAsync(command.TemplateIds, cancellationToken)).Select(t => t.Id).ToHashSet();
        var unknown = command.TemplateIds.Where(id => !known.Contains(id)).ToList();
        if (unknown.Count > 0)
            return Result<LocalPlaceDto>.Fail(AdminErrors.UnknownTemplates(unknown));

        var draft = new LocalPlaceDraft(command.Kind, command.City, command.Name, command.Address, command.Url, command.Note,
            command.StartsAt, command.EndsAt, command.TemplateIds);

        LocalPlace place;
        AdminAction action;
        if (command.Id is { } id)
        {
            var existing = await places.GetByIdAsync(id, cancellationToken);
            if (existing is null)
                return Result<LocalPlaceDto>.Fail(LocalPlaceErrors.NotFound);
            var updated = existing.Update(draft);
            if (!updated.Succeeded)
                return Result<LocalPlaceDto>.Fail(updated.Errors);
            place = existing;
            action = AdminAction.PlaceUpdated;
        }
        else
        {
            var actor = await audit.GetActorAsync(cancellationToken);
            var created = LocalPlace.Create(draft, actor.Email);
            if (!created.Succeeded)
                return Result<LocalPlaceDto>.Fail(created.Errors);
            place = created.Data!;
            await places.AddAsync(place, cancellationToken);
            action = AdminAction.PlaceCreated;
        }

        place.SetActive(command.IsActive);
        await audit.RecordAsync(action, AdminTargetType.LocalPlace, place.Id, $"{place.Name} · {place.City}", null,
            new { place.Kind, templates = place.TemplateIds.Count, place.StartsAt, place.EndsAt, place.IsActive }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LocalPlaceDto>.Ok((await LocalPlaceMapping.ToDtosAsync([place], templates, clock.GetUtcNow().UtcDateTime, cancellationToken))[0]);
    }
}

public sealed record DeleteLocalPlaceCommand(Guid Id) : ICommand;

internal sealed class DeleteLocalPlaceCommandHandler(ILocalPlaceRepository places, AdminAuditWriter audit, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteLocalPlaceCommand>
{
    public async Task<Result> Handle(DeleteLocalPlaceCommand command, CancellationToken cancellationToken)
    {
        var place = await places.GetByIdAsync(command.Id, cancellationToken);
        if (place is null)
            return Result.Fail(LocalPlaceErrors.NotFound);

        await places.DeleteAsync(place, cancellationToken);
        await audit.RecordAsync(AdminAction.PlaceDeleted, AdminTargetType.LocalPlace, place.Id, $"{place.Name} · {place.City}",
            null, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
