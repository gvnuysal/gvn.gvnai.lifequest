using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

public sealed record GetMyProfileQuery : IQuery<ProfileDto>;

internal sealed class GetMyProfileQueryHandler(
    IUserProfileRepository profiles,
    ProfileService profileService,
    IUserContext user) : IQueryHandler<GetMyProfileQuery, ProfileDto>
{
    public async Task<Result<ProfileDto>> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
        return profile is null
            ? Result<ProfileDto>.Fail(ProfileErrors.ProfileNotFound)
            : await profileService.ToDtoAsync(profile, cancellationToken);
    }
}
