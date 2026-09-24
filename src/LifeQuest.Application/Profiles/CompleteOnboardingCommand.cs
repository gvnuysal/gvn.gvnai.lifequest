using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

/// <summary>
/// Uzun bir test yerine: hedefler, ilgi alanları, haftalık süre, bütçe ve Discovery Radius.
/// Şehir isteğe bağlıdır; kesin konum istenmez.
/// </summary>
public sealed record CompleteOnboardingCommand(
    IReadOnlyList<LifeCategory> Goals,
    IReadOnlyList<InterestSelectionRequest> Interests,
    int WeeklyAvailableMinutes,
    CostBand Budget,
    DiscoveryRadius DiscoveryRadius,
    string? City,
    string? TimeZoneId) : ICommand<ProfileDto>;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.Goals).ValidGoals();
        RuleFor(x => x.Interests).ValidInterests();
        RuleFor(x => (int?)x.WeeklyAvailableMinutes).ValidWeeklyMinutes().OverridePropertyName(nameof(CompleteOnboardingCommand.WeeklyAvailableMinutes));
        RuleFor(x => x.Budget).IsInEnum();
        RuleFor(x => x.DiscoveryRadius).IsInEnum();
        RuleFor(x => x.City).ValidCity();
        RuleFor(x => x.TimeZoneId).ValidTimeZone();
    }
}

internal sealed class CompleteOnboardingCommandHandler(
    IUserProfileRepository profiles,
    ProfileService profileService,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CompleteOnboardingCommand, ProfileDto>
{
    public async Task<Result<ProfileDto>> Handle(CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
        if (profile is null)
            return Result<ProfileDto>.Fail(ProfileErrors.ProfileNotFound);

        var interests = await profileService.ResolveInterestsAsync(command.Interests, cancellationToken);
        if (!interests.Succeeded)
            return Result<ProfileDto>.Fail(interests.Errors);

        var preferences = new ProfilePreferences(
            command.DiscoveryRadius, command.Budget, command.WeeklyAvailableMinutes, command.Goals,
            command.City, ClearCity: string.IsNullOrWhiteSpace(command.City), command.TimeZoneId);

        var result = profile.CompleteOnboarding(preferences, interests.Data!, clock.GetUtcNow().UtcDateTime);
        if (!result.Succeeded)
            return Result<ProfileDto>.Fail(result.Errors);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await profileService.ToDtoAsync(profile, cancellationToken);
    }
}
