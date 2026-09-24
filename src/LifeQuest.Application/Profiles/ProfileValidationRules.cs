using FluentValidation;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Application.Profiles;

internal static class ProfileValidationRules
{
    public static void ValidInterests<T>(this IRuleBuilder<T, IReadOnlyList<InterestSelectionRequest>> rule)
        => rule
            .NotNull()
            .Must(i => i.Count is >= 1 and <= 20).WithMessage("1 ile 20 arasında ilgi alanı seçmelisin.")
            .ForEach(item => item.ChildRules(i =>
            {
                i.RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
                i.RuleFor(x => x.Weight).InclusiveBetween(0, 1).When(x => x.Weight is not null);
            }));

    public static void ValidGoals<T>(this IRuleBuilder<T, IReadOnlyList<LifeCategory>?> rule)
        => rule
            .Must(g => g is null || g.Count <= LifeCategories.All.Count)
            .ForEach(g => g.IsInEnum());

    public static IRuleBuilderOptions<T, int?> ValidWeeklyMinutes<T>(this IRuleBuilder<T, int?> rule)
        => rule.InclusiveBetween(UserProfile.MinWeeklyMinutes, UserProfile.MaxWeeklyMinutes);

    public static void ValidCity<T>(this IRuleBuilder<T, string?> rule) => rule.MaximumLength(80);

    public static void ValidTimeZone<T>(this IRuleBuilder<T, string?> rule)
        => rule
            .Must(tz => tz is null || TimeZones.IsValid(tz))
            .WithMessage("Geçersiz saat dilimi. IANA formatı kullanın (ör. Europe/Istanbul).");
}
