using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

public sealed record ProfileDto(
    Guid UserId,
    string DisplayName,
    string Email,
    bool OnboardingCompleted,
    DiscoveryRadius DiscoveryRadius,
    CostBand Budget,
    int WeeklyAvailableMinutes,
    IReadOnlyList<LifeCategory> Goals,
    string? City,
    string TimeZoneId,
    IReadOnlyList<ProfileInterestDto> Interests);

public sealed record ProfileInterestDto(string Code, string Name, LifeCategory Category, double Weight, InterestSource Source);

public sealed record InterestSelectionRequest(string Code, double? Weight);

public sealed class ProfileService(IUserAccountRepository accounts, IQuestCatalog catalog)
{
    public const double DefaultExplicitWeight = 0.7;

    public async Task<Result<ProfileDto>> ToDtoAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(profile.UserId, cancellationToken);
        if (account is null)
            return Result<ProfileDto>.Fail(IdentityErrors.AccountNotFound);

        var interests = (await catalog.GetInterestsAsync(cancellationToken)).ToDictionary(i => i.Id);

        var interestDtos = profile.Interests
            .Where(i => interests.ContainsKey(i.InterestId))
            .OrderByDescending(i => i.Weight)
            .Select(i =>
            {
                var info = interests[i.InterestId];
                return new ProfileInterestDto(info.Code, info.Name, info.Category, i.Weight, i.Source);
            })
            .ToList();

        return Result<ProfileDto>.Ok(new ProfileDto(
            account.Id, account.DisplayName, account.Email, profile.OnboardingCompleted,
            profile.DiscoveryRadius, profile.Budget, profile.WeeklyAvailableMinutes,
            profile.Goals.ToList(), profile.City, profile.TimeZoneId, interestDtos));
    }

    /// <summary>İlgi alanı kodlarını katalog id'lerine çevirir; bilinmeyen kod varsa hata döner.</summary>
    public async Task<Result<IReadOnlyCollection<InterestSelection>>> ResolveInterestsAsync(
        IReadOnlyCollection<InterestSelectionRequest> requests, CancellationToken cancellationToken)
    {
        var byCode = (await catalog.GetInterestsAsync(cancellationToken))
            .ToDictionary(i => i.Code, StringComparer.OrdinalIgnoreCase);

        var unknown = requests.Where(r => !byCode.ContainsKey(r.Code)).Select(r => r.Code).ToList();
        if (unknown.Count > 0)
            return Result<IReadOnlyCollection<InterestSelection>>.Fail(ProfileErrors.UnknownInterests(unknown));

        return Result<IReadOnlyCollection<InterestSelection>>.Ok(requests
            .Select(r => new InterestSelection(byCode[r.Code].Id, r.Weight ?? DefaultExplicitWeight))
            .ToList());
    }
}
