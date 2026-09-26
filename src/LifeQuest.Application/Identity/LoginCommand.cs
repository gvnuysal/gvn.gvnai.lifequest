using Gvn.GvnFramework.Core.Observability;
using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using Gvn.GvnFramework.Security.Abstractions;
using LifeQuest.Domain.Identity;

namespace LifeQuest.Application.Identity;

public sealed record LoginCommand([property: Sensitive(MaskMode.Partial)] string Email, string Password) : ICommand<AuthTokensDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

internal sealed class LoginCommandHandler(
    IUserAccountRepository accounts,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<LoginCommand, AuthTokensDto>
{
    // Kullanıcı bulunamadığında da hash doğrulaması yapılır: yanıt süresinden e-posta varlığı anlaşılamaz.
    private static string? _dummyHash;

    public async Task<Result<AuthTokensDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var account = await accounts.GetByEmailAsync(UserAccount.NormalizeEmail(command.Email), cancellationToken);

        if (account is null)
        {
            passwordHasher.Verify(command.Password, _dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString()));
            return Result<AuthTokensDto>.Fail(IdentityErrors.InvalidCredentials);
        }

        if (account.IsLockedOut(now))
            return Result<AuthTokensDto>.Fail(IdentityErrors.AccountLocked);

        if (!passwordHasher.Verify(command.Password, account.PasswordHash))
        {
            account.RegisterFailedLogin(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<AuthTokensDto>.Fail(IdentityErrors.InvalidCredentials);
        }

        // Askı bilgisi yalnızca şifreyi bilen kişiye gösterilir: hesap varlığı sızdırılmaz.
        if (account.IsSuspended(now))
            return Result<AuthTokensDto>.Fail(IdentityErrors.AccountSuspended(account.SuspendedUntil));

        account.RegisterSuccessfulLogin(now);
        var (tokens, _) = await tokenIssuer.IssueAsync(account, familyId: null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensDto>.Ok(tokens);
    }
}
