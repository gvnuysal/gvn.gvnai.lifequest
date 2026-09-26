using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Calendar;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Quests;

public sealed record SavedQuestDto(
    Guid TemplateId,
    string Title,
    string Description,
    LifeCategory Category,
    QuestType Type,
    int MinMinutes,
    int MaxMinutes,
    CostBand Cost,
    PhysicalEffort Effort,
    DateTime SavedAt,
    bool IsAvailable);

// ── Sonra yaparım ─────────────────────────────────────────────────────────────

/// <summary>Bir quest'in (öneri, aktif veya geçmiş) template'ini "sonra yaparım" listesine ekler. Tekrar çağrılabilir.</summary>
public sealed record SaveQuestCommand(Guid QuestId) : ICommand<SavedQuestDto>;

internal sealed class SaveQuestCommandHandler(
    IUserQuestRepository quests,
    ISavedQuestRepository saved,
    IQuestTemplateRepository templates,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<SaveQuestCommand, SavedQuestDto>
{
    public async Task<Result<SavedQuestDto>> Handle(SaveQuestCommand command, CancellationToken cancellationToken)
    {
        var quest = await quests.GetForUserAsync(command.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<SavedQuestDto>.Fail(QuestErrors.NotFound);

        var entry = await saved.GetAsync(user.UserId, quest.TemplateId, cancellationToken);
        if (entry is null)
        {
            if (await saved.CountAsync(user.UserId, cancellationToken) >= SavedQuest.MaxPerUser)
                return Result<SavedQuestDto>.Fail(QuestErrors.SavedLimitReached);

            entry = SavedQuest.Create(user.UserId, quest.TemplateId, clock.GetUtcNow().UtcDateTime);
            await saved.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var template = await templates.GetByIdAsync(quest.TemplateId, cancellationToken);
        return Result<SavedQuestDto>.Ok(SavedMapping.ToDto(entry, template));
    }
}

public sealed record GetSavedQuestsQuery : IQuery<IReadOnlyList<SavedQuestDto>>;

internal sealed class GetSavedQuestsQueryHandler(
    ISavedQuestRepository saved, IQuestTemplateRepository templates, IUserContext user)
    : IQueryHandler<GetSavedQuestsQuery, IReadOnlyList<SavedQuestDto>>
{
    public async Task<Result<IReadOnlyList<SavedQuestDto>>> Handle(GetSavedQuestsQuery query, CancellationToken cancellationToken)
    {
        var entries = await saved.GetForUserAsync(user.UserId, cancellationToken);
        var result = new List<SavedQuestDto>(entries.Count);
        foreach (var entry in entries)
            result.Add(SavedMapping.ToDto(entry, await templates.GetByIdAsync(entry.TemplateId, cancellationToken)));

        return Result<IReadOnlyList<SavedQuestDto>>.Ok(result);
    }
}

public sealed record RemoveSavedQuestCommand(Guid TemplateId) : ICommand;

internal sealed class RemoveSavedQuestCommandHandler(ISavedQuestRepository saved, IUserContext user, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveSavedQuestCommand>
{
    public async Task<Result> Handle(RemoveSavedQuestCommand command, CancellationToken cancellationToken)
    {
        var entry = await saved.GetAsync(user.UserId, command.TemplateId, cancellationToken);
        if (entry is not null)
        {
            await saved.DeleteAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}

/// <summary>Listeden başlatır: uygunluk kontrolünden geçen template kabul edilmiş bir quest olur ve listeden çıkar.</summary>
public sealed record StartSavedQuestCommand(Guid TemplateId) : ICommand<QuestDto>;

internal sealed class StartSavedQuestCommandHandler(
    ISavedQuestRepository saved,
    QuestOfferService offers,
    IUserContext user,
    IUnitOfWork unitOfWork) : ICommandHandler<StartSavedQuestCommand, QuestDto>
{
    public async Task<Result<QuestDto>> Handle(StartSavedQuestCommand command, CancellationToken cancellationToken)
    {
        var entry = await saved.GetAsync(user.UserId, command.TemplateId, cancellationToken);
        if (entry is null)
            return Result<QuestDto>.Fail(QuestErrors.SavedNotFound);

        var started = await offers.StartTemplateAsync(user.UserId, command.TemplateId, cancellationToken);
        if (!started.Succeeded)
            return Result<QuestDto>.Fail(started.Errors);

        await saved.DeleteAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<QuestDto>.Ok(started.Data!.ToDto());
    }
}

internal static class SavedMapping
{
    public static SavedQuestDto ToDto(SavedQuest entry, QuestTemplate? t) => t is null
        ? new SavedQuestDto(entry.TemplateId, Text.Of("Kaldırılmış deneyim", "Removed experience"), Text.Of("Bu deneyim artık katalogda yok.", "This experience is no longer in the catalog."), LifeCategory.Explorer,
            QuestType.Daily, 0, 0, CostBand.Free, PhysicalEffort.None, entry.SavedAt, false)
        : new SavedQuestDto(entry.TemplateId, t.Title, t.Description, t.Category, t.Type, t.MinMinutes, t.MaxMinutes, t.Cost,
            t.Effort, entry.SavedAt, t.IsOfferable);
}

// ── Planlama ve takvim ────────────────────────────────────────────────────────

/// <param name="PlannedAtLocal">Kullanıcının yerel saatiyle; <c>null</c> planı kaldırır.</param>
public sealed record PlanQuestCommand(Guid QuestId, DateTime? PlannedAtLocal) : ICommand<QuestDto>;

internal sealed class PlanQuestCommandHandler(
    IUserQuestRepository quests,
    IUserProfileRepository profiles,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<PlanQuestCommand, QuestDto>
{
    public async Task<Result<QuestDto>> Handle(PlanQuestCommand command, CancellationToken cancellationToken)
    {
        var quest = await quests.GetForUserAsync(command.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<QuestDto>.Fail(QuestErrors.NotFound);

        DateTime? plannedUtc = null;
        if (command.PlannedAtLocal is { } local)
        {
            var timeZone = (await profiles.GetByUserIdAsync(user.UserId, cancellationToken))?.ResolveTimeZone() ?? TimeZoneInfo.Utc;
            plannedUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
        }

        var result = quest.Plan(plannedUtc, clock.GetUtcNow().UtcDateTime);
        if (!result.Succeeded)
            return Result<QuestDto>.Fail(result.Errors);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<QuestDto>.Ok(quest.ToDto());
    }
}

public sealed record CalendarFile(string FileName, string Content);

public sealed record GetQuestCalendarQuery(Guid QuestId) : IQuery<CalendarFile>;

internal sealed class GetQuestCalendarQueryHandler(IUserQuestRepository quests, IUserContext user, TimeProvider clock)
    : IQueryHandler<GetQuestCalendarQuery, CalendarFile>
{
    private static readonly Error NotPlanned =
        Error.Conflict("QUEST_NOT_PLANNED", Text.Of("Takvime eklemek için önce bir zaman planla.", "Plan a time first to add it to your calendar."));

    public async Task<Result<CalendarFile>> Handle(GetQuestCalendarQuery query, CancellationToken cancellationToken)
    {
        var quest = await quests.GetForUserAsync(query.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<CalendarFile>.Fail(QuestErrors.NotFound);
        if (quest.PlannedAt is not { } start)
            return Result<CalendarFile>.Fail(NotPlanned);

        var content = IcsCalendar.Build(new CalendarEvent(
            quest.Id, quest.Title, quest.Description, start, TimeSpan.FromMinutes(quest.MaxMinutes), clock.GetUtcNow().UtcDateTime));
        return Result<CalendarFile>.Ok(new CalendarFile($"lifequest-{start:yyyyMMdd-HHmm}.ics", content));
    }
}

public sealed class PlanQuestCommandValidator : AbstractValidator<PlanQuestCommand>
{
    public PlanQuestCommandValidator()
        => RuleFor(x => x.PlannedAtLocal).Must(d => d is null || d.Value.Year is > 2000 and < 3000).WithMessage(_ => Text.Of("Geçersiz tarih.", "Invalid date."));
}
