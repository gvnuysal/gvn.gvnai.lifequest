using Gvn.GvnFramework.Core.Observability;
using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Application.Common;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Identity;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Identity;
using MediatR;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Admin.Users;

/// <summary>
/// Admin'in gördüğü kullanıcı: yalnızca hesap bilgisi ve toplam sayılar. Görev içerikleri, puanlar ve ilgiler
/// bilinçli olarak yoktur (veri minimizasyonu).
/// </summary>
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool IsSuspended,
    DateTime? SuspendedUntil,
    string? SuspensionReason,
    int CompletedQuests,
    int LifeXp,
    bool IsBootstrapAdmin,
    bool IsSelf);

internal static class AdminUserMapping
{
    public static AdminUserDto ToDto(this AdminUserRow row, DateTime nowUtc, AdminOptions options, Guid actorId)
    {
        var suspended = row.SuspendedAt is not null && (row.SuspendedUntil is null || row.SuspendedUntil > nowUtc);
        return new AdminUserDto(
            row.Id, row.Email, row.DisplayName, row.Role, row.CreatedAt, row.LastLoginAt,
            suspended, suspended ? row.SuspendedUntil : null, suspended ? row.SuspensionReason : null,
            row.CompletedQuests, row.LifeXp, options.IsBootstrapAdmin(row.Email), row.Id == actorId);
    }
}

// ── Sorgular ──────────────────────────────────────────────────────────────────

public sealed record SearchUsersQuery(string? Search, AdminUserFilter Filter, int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResult<AdminUserDto>>;

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Filter).IsInEnum();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
    }
}

internal sealed class SearchUsersQueryHandler(
    IAdminUserReader reader, IUserContext user, IOptions<AdminOptions> options, TimeProvider clock)
    : IQueryHandler<SearchUsersQuery, PagedResult<AdminUserDto>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var paging = new PagedRequest { PageNumber = query.PageNumber, PageSize = query.PageSize };
        var (items, total) = await reader.SearchAsync(
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), query.Filter, now,
            paging.Skip, paging.PageSize, cancellationToken);

        return Result<PagedResult<AdminUserDto>>.Ok(new PagedResult<AdminUserDto>(
            items.Select(r => r.ToDto(now, options.Value, user.UserId)), total, paging.PageNumber, paging.PageSize));
    }
}

public sealed record GetUserQuery(Guid UserId) : IQuery<AdminUserDto>;

internal sealed class GetUserQueryHandler(
    IAdminUserReader reader, IUserContext user, IOptions<AdminOptions> options, TimeProvider clock)
    : IQueryHandler<GetUserQuery, AdminUserDto>
{
    public async Task<Result<AdminUserDto>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var row = await reader.GetAsync(query.UserId, cancellationToken);
        return row is null
            ? Result<AdminUserDto>.Fail(IdentityErrors.AccountNotFound)
            : Result<AdminUserDto>.Ok(row.ToDto(clock.GetUtcNow().UtcDateTime, options.Value, user.UserId));
    }
}

// ── Komutlar ──────────────────────────────────────────────────────────────────

/// <param name="Days">1, 7 veya 30 gün; <c>null</c> süresiz.</param>
public sealed record SuspendUserCommand(Guid UserId, int? Days, string Reason) : ICommand<AdminUserDto>;

public sealed class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.Days).Must(d => d is null or 1 or 7 or 30).WithMessage("Askı süresi 1, 7, 30 gün veya süresiz olmalıdır.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

internal sealed class SuspendUserCommandHandler(
    IUserAccountRepository accounts,
    IRefreshTokenRepository refreshTokens,
    IAccountStateCache accountState,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    ISender sender,
    TimeProvider clock) : ICommandHandler<SuspendUserCommand, AdminUserDto>
{
    public async Task<Result<AdminUserDto>> Handle(SuspendUserCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var account = await accounts.GetByIdAsync(command.UserId, cancellationToken);
        if (account is null)
            return Result<AdminUserDto>.Fail(IdentityErrors.AccountNotFound);
        if (account.Id == audit.ActorId)
            return Result<AdminUserDto>.Fail(AdminErrors.SelfAction);
        if (account.Role == UserRoles.Admin)
            return Result<AdminUserDto>.Fail(AdminErrors.TargetIsAdmin);

        var until = command.Days is { } days ? now.AddDays(days) : (DateTime?)null;
        account.Suspend(until, command.Reason, now);
        await audit.RecordAsync(AdminAction.UserSuspended, AdminTargetType.User, account.Id, account.Email,
            command.Reason, new { until }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Açık oturumlar hemen kapanır: refresh token'lar iptal, access token'lar middleware'de reddedilir.
        await refreshTokens.RevokeAllForUserAsync(account.Id, now, "suspended", cancellationToken);
        await accountState.InvalidateAsync(account.Id, cancellationToken);

        return await sender.Send(new GetUserQuery(account.Id), cancellationToken);
    }
}

public sealed record UnsuspendUserCommand(Guid UserId) : ICommand<AdminUserDto>;

internal sealed class UnsuspendUserCommandHandler(
    IUserAccountRepository accounts,
    IAccountStateCache accountState,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    ISender sender) : ICommandHandler<UnsuspendUserCommand, AdminUserDto>
{
    public async Task<Result<AdminUserDto>> Handle(UnsuspendUserCommand command, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(command.UserId, cancellationToken);
        if (account is null)
            return Result<AdminUserDto>.Fail(IdentityErrors.AccountNotFound);

        if (account.Unsuspend())
        {
            await audit.RecordAsync(AdminAction.UserUnsuspended, AdminTargetType.User, account.Id, account.Email,
                null, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await accountState.InvalidateAsync(account.Id, cancellationToken);
        }

        return await sender.Send(new GetUserQuery(account.Id), cancellationToken);
    }
}

public sealed record SetUserRoleCommand(Guid UserId, string Role) : ICommand<AdminUserDto>;

public sealed class SetUserRoleCommandValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleCommandValidator()
        => RuleFor(x => x.Role).Must(r => r is UserRoles.User or UserRoles.Admin).WithMessage("Rol 'user' veya 'admin' olmalıdır.");
}

internal sealed class SetUserRoleCommandHandler(
    IUserAccountRepository accounts,
    IAccountStateCache accountState,
    IOptions<AdminOptions> options,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    ISender sender) : ICommandHandler<SetUserRoleCommand, AdminUserDto>
{
    public async Task<Result<AdminUserDto>> Handle(SetUserRoleCommand command, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(command.UserId, cancellationToken);
        if (account is null)
            return Result<AdminUserDto>.Fail(IdentityErrors.AccountNotFound);
        if (account.Id == audit.ActorId)
            return Result<AdminUserDto>.Fail(AdminErrors.SelfAction);

        if (command.Role == UserRoles.User && account.Role == UserRoles.Admin)
        {
            if (options.Value.IsBootstrapAdmin(account.Email))
                return Result<AdminUserDto>.Fail(AdminErrors.BootstrapAdmin);
            if (await accounts.CountByRoleAsync(UserRoles.Admin, cancellationToken) <= 1)
                return Result<AdminUserDto>.Fail(AdminErrors.LastAdmin);
        }

        var before = account.Role;
        if (account.GrantRole(command.Role))
        {
            await audit.RecordAsync(AdminAction.UserRoleChanged, AdminTargetType.User, account.Id, account.Email,
                null, new { before, after = command.Role }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Eski rolü taşıyan access token bir sonraki istekte TOKEN_STALE alır ve istemci yeni rolle yeniler.
            await accountState.InvalidateAsync(account.Id, cancellationToken);
        }

        return await sender.Send(new GetUserQuery(account.Id), cancellationToken);
    }
}

public sealed record DeleteUserCommand(Guid UserId, string Reason, [property: Sensitive(MaskMode.Partial)] string ConfirmEmail) : ICommand;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ConfirmEmail).NotEmpty().MaximumLength(254);
    }
}

internal sealed class DeleteUserCommandHandler(
    IUserAccountRepository accounts,
    IAccountStateCache accountState,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(command.UserId, cancellationToken);
        if (account is null)
            return Result.Fail(IdentityErrors.AccountNotFound);
        if (account.Id == audit.ActorId)
            return Result.Fail(AdminErrors.SelfAction);
        if (account.Role == UserRoles.Admin)
            return Result.Fail(AdminErrors.TargetIsAdmin);
        if (UserAccount.NormalizeEmail(command.ConfirmEmail) != account.Email)
            return Result.Fail(AdminErrors.ConfirmEmailMismatch);

        // Kullanıcı verisi cascade ile silinir (profil, görevler, XP, özetler, oturumlar). Denetim kaydında
        // yalnızca maskelenmiş e-posta kalır.
        await audit.RecordAsync(AdminAction.UserDeleted, AdminTargetType.User, account.Id, EmailMask.Mask(account.Email),
            command.Reason, null, cancellationToken);
        await accounts.DeleteAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await accountState.InvalidateAsync(account.Id, cancellationToken);

        return Result.Ok();
    }
}
