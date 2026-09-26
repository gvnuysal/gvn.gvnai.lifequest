using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Application.Common;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Admin;
using LifeQuest.Application.Safety;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Community;

namespace LifeQuest.Application.Community;

/// <summary>Kullanıcının kendi fikrini gördüğü hal: kimin incelediği gösterilmez, yalnızca sonuç ve not.</summary>
public sealed record MyIdeaDto(
    Guid Id,
    string Title,
    string Description,
    LifeCategory Category,
    int Minutes,
    CostBand Cost,
    bool IsOutdoor,
    IdeaStatus Status,
    string? ReviewNote,
    DateTime SubmittedAt,
    DateTime? ReviewedAt);

/// <summary>Admin görünümü: gönderenin kimliği yoktur (fikir içeriğine göre karar verilir).</summary>
public sealed record AdminIdeaDto(
    Guid Id,
    string Title,
    string Description,
    LifeCategory Category,
    int Minutes,
    CostBand Cost,
    bool IsOutdoor,
    IdeaStatus Status,
    IReadOnlyList<string> Flags,
    string? ReviewNote,
    string? ReviewedBy,
    Guid? TemplateId,
    DateTime SubmittedAt,
    DateTime? ReviewedAt);

internal static class IdeaMapping
{
    public static MyIdeaDto ToMine(this QuestIdea i) => new(
        i.Id, i.Title, i.Description, i.Category, i.Minutes, i.Cost, i.IsOutdoor, i.Status, i.ReviewNote, i.SubmittedAt, i.ReviewedAt);

    public static AdminIdeaDto ToAdmin(this QuestIdea i) => new(
        i.Id, i.Title, i.Description, i.Category, i.Minutes, i.Cost, i.IsOutdoor, i.Status, i.Flags, i.ReviewNote,
        i.ReviewedBy, i.TemplateId, i.SubmittedAt, i.ReviewedAt);
}

// ── Kullanıcı ─────────────────────────────────────────────────────────────────

public sealed record SubmitIdeaCommand(
    string Title, string Description, LifeCategory Category, int Minutes, CostBand Cost, bool IsOutdoor) : ICommand<MyIdeaDto>;

public sealed class SubmitIdeaCommandValidator : AbstractValidator<SubmitIdeaCommand>
{
    public SubmitIdeaCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().Length(CatalogSafetyRules.MinTitleLength, CatalogSafetyRules.MaxTitleLength);
        RuleFor(x => x.Description).NotEmpty().Length(CatalogSafetyRules.MinDescriptionLength, CatalogSafetyRules.MaxDescriptionLength);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Cost).IsInEnum();
        RuleFor(x => x.Minutes).InclusiveBetween(5, 600);
    }
}

internal sealed class SubmitIdeaCommandHandler(
    IQuestIdeaRepository ideas, IUserContext user, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<SubmitIdeaCommand, MyIdeaDto>
{
    public async Task<Result<MyIdeaDto>> Handle(SubmitIdeaCommand command, CancellationToken cancellationToken)
    {
        var text = $"{command.Title} {command.Description}";
        if (ContentScreen.ContainsUrl(text) || ContentScreen.ContainsContactInfo(text) || ContentScreen.ContainsMarkup(text))
            return Result<MyIdeaDto>.Fail(IdeaErrors.NoLinksOrContacts);

        var now = clock.GetUtcNow().UtcDateTime;
        if (await ideas.CountPendingAsync(user.UserId, cancellationToken) >= QuestIdea.MaxPendingPerUser)
            return Result<MyIdeaDto>.Fail(IdeaErrors.TooManyPending);
        if (await ideas.CountSubmittedSinceAsync(user.UserId, now.AddDays(-1), cancellationToken) >= QuestIdea.MaxPerDay)
            return Result<MyIdeaDto>.Fail(IdeaErrors.DailyLimit);

        // Bayraklar kod olarak saklanır; yönetim paneli kendi dilinde etiketler.
        var flags = new List<string>();
        if (ContentScreen.ContainsRiskyContent(text)) flags.Add(IdeaFlags.RiskyContent);
        if (ContentScreen.RequestsPersonalData(text)) flags.Add(IdeaFlags.PersonalData);
        if (command.IsOutdoor && command.Minutes > 240) flags.Add(IdeaFlags.LongOutdoor);

        var idea = QuestIdea.Submit(user.UserId, command.Title, command.Description, command.Category, command.Minutes,
            command.Cost, command.IsOutdoor, flags, now);
        await ideas.AddAsync(idea, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<MyIdeaDto>.Ok(idea.ToMine());
    }
}

public sealed record GetMyIdeasQuery : IQuery<IReadOnlyList<MyIdeaDto>>;

internal sealed class GetMyIdeasQueryHandler(IQuestIdeaRepository ideas, IUserContext user)
    : IQueryHandler<GetMyIdeasQuery, IReadOnlyList<MyIdeaDto>>
{
    public async Task<Result<IReadOnlyList<MyIdeaDto>>> Handle(GetMyIdeasQuery query, CancellationToken cancellationToken)
        => Result<IReadOnlyList<MyIdeaDto>>.Ok(
            (await ideas.GetForUserAsync(user.UserId, cancellationToken)).Select(i => i.ToMine()).ToList());
}

/// <summary>Yalnızca henüz incelenmemiş kendi fikrini geri çekebilir.</summary>
public sealed record WithdrawIdeaCommand(Guid IdeaId) : ICommand;

internal sealed class WithdrawIdeaCommandHandler(IQuestIdeaRepository ideas, IUserContext user, IUnitOfWork unitOfWork)
    : ICommandHandler<WithdrawIdeaCommand>
{
    public async Task<Result> Handle(WithdrawIdeaCommand command, CancellationToken cancellationToken)
    {
        var idea = await ideas.GetByIdAsync(command.IdeaId, cancellationToken);
        if (idea is null || idea.UserId != user.UserId)
            return Result.Fail(IdeaErrors.NotFound);
        if (idea.Status != IdeaStatus.Pending)
            return Result.Fail(IdeaErrors.AlreadyReviewed);

        await ideas.DeleteAsync(idea, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

// ── Admin ─────────────────────────────────────────────────────────────────────

public sealed record SearchIdeasQuery(IdeaStatus? Status, int PageNumber = 1, int PageSize = 20) : IQuery<PagedResult<AdminIdeaDto>>;

internal sealed class SearchIdeasQueryHandler(IQuestIdeaRepository ideas) : IQueryHandler<SearchIdeasQuery, PagedResult<AdminIdeaDto>>
{
    public async Task<Result<PagedResult<AdminIdeaDto>>> Handle(SearchIdeasQuery query, CancellationToken cancellationToken)
    {
        var paging = new PagedRequest { PageNumber = query.PageNumber, PageSize = query.PageSize };
        var (items, total) = await ideas.GetPageAsync(query.Status, paging.Skip, paging.PageSize, cancellationToken);
        return Result<PagedResult<AdminIdeaDto>>.Ok(new PagedResult<AdminIdeaDto>(
            items.Select(i => i.ToAdmin()), total, paging.PageNumber, paging.PageSize));
    }
}

public sealed record GetIdeaQuery(Guid IdeaId) : IQuery<AdminIdeaDto>;

internal sealed class GetIdeaQueryHandler(IQuestIdeaRepository ideas) : IQueryHandler<GetIdeaQuery, AdminIdeaDto>
{
    public async Task<Result<AdminIdeaDto>> Handle(GetIdeaQuery query, CancellationToken cancellationToken)
        => await ideas.GetByIdAsync(query.IdeaId, cancellationToken) is { } idea
            ? Result<AdminIdeaDto>.Ok(idea.ToAdmin())
            : Result<AdminIdeaDto>.Fail(IdeaErrors.NotFound);
}

/// <summary>Reddetme notu kullanıcıya gösterilir; nazik ve yönlendirici yazılmalıdır.</summary>
public sealed record RejectIdeaCommand(Guid IdeaId, string Note) : ICommand<AdminIdeaDto>;

public sealed class RejectIdeaCommandValidator : AbstractValidator<RejectIdeaCommand>
{
    public RejectIdeaCommandValidator() => RuleFor(x => x.Note).NotEmpty().MaximumLength(300);
}

internal sealed class RejectIdeaCommandHandler(
    IQuestIdeaRepository ideas, AdminAuditWriter audit, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RejectIdeaCommand, AdminIdeaDto>
{
    public async Task<Result<AdminIdeaDto>> Handle(RejectIdeaCommand command, CancellationToken cancellationToken)
    {
        var idea = await ideas.GetByIdAsync(command.IdeaId, cancellationToken);
        if (idea is null)
            return Result<AdminIdeaDto>.Fail(IdeaErrors.NotFound);

        var actor = await audit.GetActorAsync(cancellationToken);
        var result = idea.Reject(actor.Email, command.Note, clock.GetUtcNow().UtcDateTime);
        if (!result.Succeeded)
            return Result<AdminIdeaDto>.Fail(result.Errors);

        await audit.RecordAsync(AdminAction.IdeaRejected, AdminTargetType.QuestIdea, idea.Id, idea.Title, command.Note, null,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AdminIdeaDto>.Ok(idea.ToAdmin());
    }
}

public static class IdeaFlags
{
    public const string RiskyContent = "risky_content";
    public const string PersonalData = "personal_data";
    public const string LongOutdoor = "long_outdoor";
}
