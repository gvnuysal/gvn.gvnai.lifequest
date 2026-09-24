using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

public enum StarterReactionType
{
    Like = 1,
    Dislike = 2
}

/// <summary>Cold start kartına verilen tepki ("bana göre" / "bana göre değil").</summary>
public sealed record StarterReaction(string TemplateCode, StarterReactionType Reaction);

/// <summary>
/// Uzun bir test yerine: hedefler, ilgi alanları, cold start kart tepkileri, haftalık süre, bütçe, efor sınırı
/// ve Discovery Radius. Şehir isteğe bağlıdır; kesin konum istenmez.
/// </summary>
public sealed record CompleteOnboardingCommand(
    IReadOnlyList<LifeCategory> Goals,
    IReadOnlyList<InterestSelectionRequest> Interests,
    int WeeklyAvailableMinutes,
    CostBand Budget,
    DiscoveryRadius DiscoveryRadius,
    string? City,
    string? TimeZoneId,
    PhysicalEffort? MaxPhysicalEffort = null,
    IReadOnlyList<StarterReaction>? StarterReactions = null) : ICommand<ProfileDto>;

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
        RuleFor(x => x.MaxPhysicalEffort).IsInEnum();
        RuleFor(x => x.StarterReactions)
            .Must(r => r is null || r.Count <= 30).WithMessage("En fazla 30 kart tepkisi gönderilebilir.")
            .ForEach(item => item.ChildRules(r =>
            {
                r.RuleFor(x => x.TemplateCode).NotEmpty().MaximumLength(64);
                r.RuleFor(x => x.Reaction).IsInEnum();
            }))
            .When(x => x.StarterReactions is not null);
    }
}

internal sealed class CompleteOnboardingCommandHandler(
    IUserProfileRepository profiles,
    ProfileService profileService,
    IQuestCatalog catalog,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CompleteOnboardingCommand, ProfileDto>
{
    public async Task<Result<ProfileDto>> Handle(CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var profile = await profiles.GetByUserIdAsync(user.UserId, cancellationToken);
        if (profile is null)
            return Result<ProfileDto>.Fail(ProfileErrors.ProfileNotFound);

        var interests = await profileService.ResolveInterestsAsync(command.Interests, cancellationToken);
        if (!interests.Succeeded)
            return Result<ProfileDto>.Fail(interests.Errors);

        var preferences = new ProfilePreferences(
            command.DiscoveryRadius, command.Budget, command.WeeklyAvailableMinutes, command.Goals,
            command.City, ClearCity: string.IsNullOrWhiteSpace(command.City), command.TimeZoneId,
            command.MaxPhysicalEffort);

        var result = profile.CompleteOnboarding(preferences, interests.Data!, now);
        if (!result.Succeeded)
            return Result<ProfileDto>.Fail(result.Errors);

        await ApplyStarterReactionsAsync(profile, command.StarterReactions, now, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await profileService.ToDtoAsync(profile, cancellationToken);
    }

    /// <summary>
    /// Cold start: örnek kartlara verilen tepkiler ilk günden ilgi ağırlıklarını besler. "Bana göre" kullanıcının
    /// seçmediği komşu ilgileri Learned olarak ekleyebilir; "bana göre değil" yalnızca mevcut ilgileri düşürür.
    /// Bilinmeyen kodlar sessizce yok sayılır (kart listesi değişmiş olabilir).
    /// </summary>
    private async Task ApplyStarterReactionsAsync(
        UserProfile profile, IReadOnlyList<StarterReaction>? reactions, DateTime now, CancellationToken cancellationToken)
    {
        if (reactions is not { Count: > 0 })
            return;

        var cards = (await catalog.GetStarterCardsAsync(cancellationToken))
            .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var reaction in reactions.DistinctBy(r => r.TemplateCode, StringComparer.OrdinalIgnoreCase))
        {
            if (!cards.TryGetValue(reaction.TemplateCode, out var card))
                continue;

            var delta = reaction.Reaction == StarterReactionType.Like
                ? InterestLearning.StarterLikeDelta
                : InterestLearning.StarterDislikeDelta;
            profile.AdjustInterests(card.InterestIds, delta, now);
        }
    }
}
