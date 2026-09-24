using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

/// <summary>Açıkça seçilen ilgi alanlarını değiştirir; geri bildirimden öğrenilenler korunur.</summary>
public sealed record SetInterestsCommand(IReadOnlyList<InterestSelectionRequest> Interests) : ICommand<ProfileDto>;

public sealed class SetInterestsCommandValidator : AbstractValidator<SetInterestsCommand>
{
    public SetInterestsCommandValidator() => RuleFor(x => x.Interests).ValidInterests();
}

internal sealed class SetInterestsCommandHandler(
    IUserProfileRepository profiles,
    ProfileService profileService,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<SetInterestsCommand, ProfileDto>
{
    public async Task<Result<ProfileDto>> Handle(SetInterestsCommand command, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
        if (profile is null)
            return Result<ProfileDto>.Fail(ProfileErrors.ProfileNotFound);

        var interests = await profileService.ResolveInterestsAsync(command.Interests, cancellationToken);
        if (!interests.Succeeded)
            return Result<ProfileDto>.Fail(interests.Errors);

        profile.SetExplicitInterests(interests.Data!, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await profileService.ToDtoAsync(profile, cancellationToken);
    }
}
