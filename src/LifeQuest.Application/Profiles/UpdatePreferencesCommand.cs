using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

/// <summary>Kısmi güncelleme: null alanlar değişmez. Şehri silmek için <see cref="ClearCity"/> = true.</summary>
public sealed record UpdatePreferencesCommand(
    DiscoveryRadius? DiscoveryRadius,
    CostBand? Budget,
    int? WeeklyAvailableMinutes,
    IReadOnlyList<LifeCategory>? Goals,
    string? City,
    bool? ClearCity,
    string? TimeZoneId,
    PhysicalEffort? MaxPhysicalEffort = null,
    NotificationPreference? NotificationPreference = null,
    int? DailyReminderHour = null,
    bool? ClearDailyReminder = null) : ICommand<ProfileDto>;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.DiscoveryRadius).IsInEnum();
        RuleFor(x => x.Budget).IsInEnum();
        RuleFor(x => x.WeeklyAvailableMinutes).ValidWeeklyMinutes();
        RuleFor(x => x.Goals).ValidGoals();
        RuleFor(x => x.City).ValidCity();
        RuleFor(x => x.TimeZoneId).ValidTimeZone();
        RuleFor(x => x.MaxPhysicalEffort).IsInEnum();
        RuleFor(x => x.NotificationPreference).IsInEnum();
        RuleFor(x => x.DailyReminderHour)
            .InclusiveBetween(DailyReminder.EarliestHour, DailyReminder.LatestHour)
            .When(x => x.DailyReminderHour is not null);
    }
}

internal sealed class UpdatePreferencesCommandHandler(
    IUserProfileRepository profiles,
    ProfileService profileService,
    IUserContext user,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdatePreferencesCommand, ProfileDto>
{
    public async Task<Result<ProfileDto>> Handle(UpdatePreferencesCommand command, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
        if (profile is null)
            return Result<ProfileDto>.Fail(ProfileErrors.ProfileNotFound);

        var result = profile.UpdatePreferences(new ProfilePreferences(
            command.DiscoveryRadius, command.Budget, command.WeeklyAvailableMinutes, command.Goals,
            command.City, command.ClearCity ?? false, command.TimeZoneId,
            command.MaxPhysicalEffort, command.NotificationPreference,
            command.DailyReminderHour, command.ClearDailyReminder ?? false));

        if (!result.Succeeded)
            return Result<ProfileDto>.Fail(result.Errors);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await profileService.ToDtoAsync(profile, cancellationToken);
    }
}
