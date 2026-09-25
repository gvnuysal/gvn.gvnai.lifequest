using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Application.Identity;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthTokensDto>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
}

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserAccountRepository accounts,
    AuthTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<RefreshTokenCommandHandler> logger) : ICommandHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var existing = await refreshTokens.GetByHashAsync(AuthTokenIssuer.Hash(command.RefreshToken), cancellationToken);

        if (existing is null)
            return Result<AuthTokensDto>.Fail(IdentityErrors.InvalidRefreshToken);

        if (existing.IsRevoked)
        {
            // Rotasyonla iptal edilmiş bir token tekrar geldi: token sızmış olabilir. Tüm oturum ailesi kapatılır.
            logger.LogWarning("Refresh token reuse detected for user {UserId}, family {FamilyId}", existing.UserId, existing.FamilyId);
            await refreshTokens.RevokeFamilyAsync(existing.FamilyId, now, "reuse-detected", cancellationToken);
            return Result<AuthTokensDto>.Fail(IdentityErrors.InvalidRefreshToken);
        }

        if (!existing.IsActive(now))
            return Result<AuthTokensDto>.Fail(IdentityErrors.InvalidRefreshToken);

        var account = await accounts.GetByIdAsync(existing.UserId, cancellationToken);
        if (account is null || account.IsLockedOut(now))
            return Result<AuthTokensDto>.Fail(IdentityErrors.InvalidRefreshToken);
        if (account.IsSuspended(now))
            return Result<AuthTokensDto>.Fail(IdentityErrors.AccountSuspended(account.SuspendedUntil));

        var (tokens, replacement) = await tokenIssuer.IssueAsync(account, existing.FamilyId, cancellationToken);
        existing.Revoke(now, "rotated", replacement.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensDto>.Ok(tokens);
    }
}
