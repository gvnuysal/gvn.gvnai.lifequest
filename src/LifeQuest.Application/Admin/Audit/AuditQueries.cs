using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Application.Common;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Domain.Admin;

namespace LifeQuest.Application.Admin.Audit;

public sealed record AuditEntryDto(
    Guid Id,
    DateTime CreatedAt,
    string ActorEmail,
    AdminAction Action,
    AdminTargetType TargetType,
    Guid? TargetId,
    string TargetLabel,
    string? Reason,
    string? Details);

public sealed record GetAuditLogQuery(AdminAction? Action, AdminTargetType? TargetType, int PageNumber = 1, int PageSize = 30)
    : IQuery<PagedResult<AuditEntryDto>>;

public sealed class GetAuditLogQueryValidator : AbstractValidator<GetAuditLogQuery>
{
    public GetAuditLogQueryValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
    }
}

internal sealed class GetAuditLogQueryHandler(IAdminAuditRepository audit) : IQueryHandler<GetAuditLogQuery, PagedResult<AuditEntryDto>>
{
    public async Task<Result<PagedResult<AuditEntryDto>>> Handle(GetAuditLogQuery query, CancellationToken cancellationToken)
    {
        var paging = new PagedRequest { PageNumber = query.PageNumber, PageSize = query.PageSize };
        var (items, total) = await audit.GetPageAsync(query.Action, query.TargetType, paging.Skip, paging.PageSize, cancellationToken);

        return Result<PagedResult<AuditEntryDto>>.Ok(new PagedResult<AuditEntryDto>(
            items.Select(e => new AuditEntryDto(e.Id, e.CreatedAt, e.ActorEmail, e.Action, e.TargetType, e.TargetId,
                e.TargetLabel, e.Reason, e.Details)),
            total, paging.PageNumber, paging.PageSize));
    }
}
