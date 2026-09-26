using Gvn.GvnFramework.Core.Observability;
using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using Gvn.GvnFramework.Security.Abstractions;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using Microsoft.Extensions.Options;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Identity;

/// <param name="Language">"tr" / "en"; verilmezse isteğin dili (Accept-Language).</param>
public sealed record RegisterCommand(
    [property: Sensitive(MaskMode.Partial)] string Email, string Password, string DisplayName, int BirthYear, string? Language = null)
    : ICommand<AuthTokensDto>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(TimeProvider clock)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.DisplayName).NotEmpty().Length(2, 50);
        RuleFor(x => x.BirthYear).InclusiveBetween(1900, clock.GetUtcNow().Year);
        RuleFor(x => x.Language).Must(Domain.Localization.Language.IsSupported)
            .When(x => x.Language is not null)
            .WithMessage(_ => Text.Of("Desteklenen diller: tr, en.", "Supported languages: tr, en."));
    }
}

public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty()
            .Length(8, 128)
            .Must(p => p is not null && p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage(_ => Text.Of("Şifre en az bir harf ve bir rakam içermelidir.", "The password must contain at least one letter and one digit."));
}

internal sealed class RegisterCommandHandler(
    IUserAccountRepository accounts,
    IUserProfileRepository profiles,
    IPlayerProgressRepository progress,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    IOptions<AdminOptions> adminOptions,
    TimeProvider clock) : ICommandHandler<RegisterCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = UserAccount.NormalizeEmail(command.Email);
        if (await accounts.EmailExistsAsync(email, cancellationToken))
            return Result<AuthTokensDto>.Fail(IdentityErrors.EmailTaken);

        var registration = UserAccount.Register(
            email, passwordHasher.Hash(command.Password), command.DisplayName, command.BirthYear,
            clock.GetUtcNow().UtcDateTime, command.Language ?? Domain.Localization.Language.Current);

        if (!registration.Succeeded)
            return Result<AuthTokensDto>.Fail(registration.Errors);

        var account = registration.Data!;
        if (adminOptions.Value.IsBootstrapAdmin(account.Email))
            account.GrantRole(UserRoles.Admin);
        await accounts.AddAsync(account, cancellationToken);
        await profiles.AddAsync(UserProfile.CreateFor(account.Id), cancellationToken);
        await progress.AddAsync(PlayerProgress.CreateFor(account.Id), cancellationToken);

        var (tokens, _) = await tokenIssuer.IssueAsync(account, familyId: null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensDto>.Ok(tokens);
    }
}
