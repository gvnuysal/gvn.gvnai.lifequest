using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;

namespace LifeQuest.Application.Progression;

public sealed record GetProgressQuery : IQuery<ProgressDto>;

internal sealed class GetProgressQueryHandler(IPlayerProgressRepository progressRepository, IUserContext user)
    : IQueryHandler<GetProgressQuery, ProgressDto>
{
    public async Task<Result<ProgressDto>> Handle(GetProgressQuery query, CancellationToken cancellationToken)
    {
        var progress = await progressRepository.GetByUserIdAsync(user.UserId, cancellationToken);
        if (progress is null)
            return Result<ProgressDto>.Fail(ProfileErrors.ProfileNotFound);

        var recent = await progressRepository.GetRecentTransactionsAsync(user.UserId, 10, cancellationToken);

        var current = LevelCurve.CumulativeXpForLevel(progress.LifeLevel, LevelCurve.LifeBase);
        var next = LevelCurve.CumulativeXpForLevel(progress.LifeLevel + 1, LevelCurve.LifeBase);
        var levelProgress = next > current ? Math.Round((progress.LifeXp - current) / (double)(next - current), 3) : 1;

        var categories = progress.Categories
            .OrderBy(c => c.Category)
            .Select(c => new CategoryProgressDto(
                c.Category, c.Category.DisplayName(), c.Xp, c.Level,
                LevelCurve.CumulativeXpForLevel(c.Level + 1, LevelCurve.CategoryBase), c.CompletedCount))
            .ToList();

        var xp = recent
            .Select(t => new XpEntryDto(t.CreatedAt, t.LocalizedDescription.Current, t.LifeXp, t.PrimaryCategory, t.PrimaryCategoryXp))
            .ToList();

        return Result<ProgressDto>.Ok(new ProgressDto(
            progress.LifeXp, progress.LifeLevel, current, next, levelProgress, progress.TotalCompleted, categories, xp));
    }
}

public sealed record GetAchievementsQuery : IQuery<IReadOnlyList<AchievementDto>>;

internal sealed class GetAchievementsQueryHandler(IPlayerProgressRepository progressRepository, IUserContext user)
    : IQueryHandler<GetAchievementsQuery, IReadOnlyList<AchievementDto>>
{
    public async Task<Result<IReadOnlyList<AchievementDto>>> Handle(GetAchievementsQuery query, CancellationToken cancellationToken)
    {
        var progress = await progressRepository.GetByUserIdAsync(user.UserId, cancellationToken);
        if (progress is null)
            return Result<IReadOnlyList<AchievementDto>>.Fail(ProfileErrors.ProfileNotFound);

        var unlocked = progress.Achievements.ToDictionary(a => a.Code, a => a.UnlockedAt);

        return Result<IReadOnlyList<AchievementDto>>.Ok(AchievementCatalog.All
            .Select(a => a.ToDto(unlocked.TryGetValue(a.Code, out var at) ? at : null))
            .OrderByDescending(a => a.Unlocked)
            .ToList());
    }
}
