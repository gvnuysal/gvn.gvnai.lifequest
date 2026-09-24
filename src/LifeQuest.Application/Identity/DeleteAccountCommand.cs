using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using Gvn.GvnFramework.Security.Abstractions;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Identity;

namespace LifeQuest.Application.Identity;

/// <summary>
/// KVKK/GDPR "unutulma hakkı": hesap ve ona bağlı tüm veriler (profil, quest geçmişi, XP defteri, token'lar)
/// veritabanı seviyesinde cascade ile kalıcı olarak silinir. Şifre tekrar doğrulanır.
/// </summary>
public sealed record DeleteAccountCommand(string Password) : ICommand;

public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator() => RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
}

internal sealed class DeleteAccountCommandHandler(
    IUserAccountRepository accounts,
    IPasswordHasher passwordHasher,
    IUserContext user,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteAccountCommand>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(user.UserId, cancellationToken);
        if (account is null)
            return Result.Fail(IdentityErrors.AccountNotFound);

        if (!passwordHasher.Verify(command.Password, account.PasswordHash))
            return Result.Fail(IdentityErrors.InvalidCredentials);

        await accounts.DeleteAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
