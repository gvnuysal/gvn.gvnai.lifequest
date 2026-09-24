using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Notifications;

namespace LifeQuest.Domain.Profiles;

public sealed class UserProfile : AggregateRoot
{
    public const int MinWeeklyMinutes = 30;
    public const int MaxWeeklyMinutes = 40 * 60;

    private readonly List<UserInterest> _interests = [];

    public Guid UserId { get; private set; }
    public bool OnboardingCompleted { get; private set; }
    public DiscoveryRadius DiscoveryRadius { get; private set; } = DiscoveryRadius.Explore;
    public CostBand Budget { get; private set; } = CostBand.Low;
    public int WeeklyAvailableMinutes { get; private set; } = 180;
    public List<LifeCategory> Goals { get; private set; } = [];

    /// <summary>İsteğe bağlı. Kesin konum tutulmaz; şehir bilgisi yalnızca şehir gerektiren quest'ler için kullanılır.</summary>
    public string? City { get; private set; }

    public string TimeZoneId { get; private set; } = TimeZones.Default;

    /// <summary>Erişilebilirlik / hareket kısıtı: bu seviyenin üzerindeki eforlu quest'ler önerilmez.</summary>
    public PhysicalEffort MaxPhysicalEffort { get; private set; } = PhysicalEffort.Vigorous;

    /// <summary>Varsayılan: haftalık uygulama içi özet. Kullanıcıyı geri çağıran agresif bildirim yok.</summary>
    public NotificationPreference NotificationPreference { get; private set; } = NotificationPreference.WeeklySummary;

    public IReadOnlyCollection<UserInterest> Interests => _interests.AsReadOnly();

    private UserProfile() { }

    public static UserProfile CreateFor(Guid userId) => new() { UserId = userId };

    public Result CompleteOnboarding(ProfilePreferences preferences, IReadOnlyCollection<InterestSelection> interests, DateTime nowUtc)
    {
        if (interests.Count == 0)
            return Result.Fail(ProfileErrors.InterestsRequired);

        var result = UpdatePreferences(preferences);
        if (!result.Succeeded)
            return result;

        SetExplicitInterests(interests, nowUtc);

        if (!OnboardingCompleted)
        {
            OnboardingCompleted = true;
            AddDomainEvent(new OnboardingCompletedEvent(UserId));
        }

        return Result.Ok();
    }

    public Result UpdatePreferences(ProfilePreferences preferences)
    {
        if (preferences.WeeklyAvailableMinutes is { } minutes &&
            (minutes < MinWeeklyMinutes || minutes > MaxWeeklyMinutes))
            return Result.Fail(ProfileErrors.InvalidWeeklyMinutes);

        if (preferences.TimeZoneId is { } tz && !TimeZones.IsValid(tz))
            return Result.Fail(ProfileErrors.InvalidTimeZone);

        if (preferences.DiscoveryRadius is { } radius) DiscoveryRadius = radius;
        if (preferences.Budget is { } budget) Budget = budget;
        if (preferences.WeeklyAvailableMinutes is { } weekly) WeeklyAvailableMinutes = weekly;
        if (preferences.Goals is { } goals) Goals = goals.Distinct().ToList();
        if (preferences.TimeZoneId is { } timeZoneId) TimeZoneId = timeZoneId;
        if (preferences.MaxPhysicalEffort is { } effort) MaxPhysicalEffort = effort;
        if (preferences.Notifications is { } notifications) NotificationPreference = notifications;
        if (preferences.ClearCity) City = null;
        else if (!string.IsNullOrWhiteSpace(preferences.City)) City = preferences.City.Trim();

        return Result.Ok();
    }

    /// <summary>
    /// Kullanıcının açıkça seçtiği ilgi alanlarını ayarlar. Listede olmayan explicit ilgiler kaldırılır,
    /// öğrenilmiş (Learned) ilgiler korunur.
    /// </summary>
    public void SetExplicitInterests(IReadOnlyCollection<InterestSelection> selections, DateTime nowUtc)
    {
        var selected = selections
            .GroupBy(s => s.InterestId)
            .ToDictionary(g => g.Key, g => g.Last().Weight);

        _interests.RemoveAll(i => i.Source == InterestSource.Explicit && !selected.ContainsKey(i.InterestId));

        foreach (var (interestId, weight) in selected)
        {
            var existing = _interests.FirstOrDefault(i => i.InterestId == interestId);
            if (existing is null)
                _interests.Add(new UserInterest(Id, interestId, weight, InterestSource.Explicit, nowUtc));
            else
                existing.SetExplicit(weight, nowUtc);
        }
    }

    /// <summary>
    /// Geri bildirimden öğrenme. Olumlu sinyalde kullanıcıda olmayan ilgi alanı Learned olarak eklenir;
    /// olumsuz sinyal yalnızca mevcut ilgileri düşürür.
    /// </summary>
    public void AdjustInterests(IEnumerable<Guid> interestIds, double delta, DateTime nowUtc)
    {
        if (delta == 0)
            return;

        foreach (var interestId in interestIds.Distinct())
        {
            var existing = _interests.FirstOrDefault(i => i.InterestId == interestId);
            if (existing is not null)
                existing.Adjust(delta, nowUtc);
            else if (delta > 0)
                _interests.Add(new UserInterest(
                    Id, interestId, InterestLearning.NewLearnedInterestBase + delta, InterestSource.Learned, nowUtc));
        }
    }

    public IReadOnlyDictionary<Guid, double> InterestWeights()
        => _interests.ToDictionary(i => i.InterestId, i => i.Weight);

    public TimeZoneInfo ResolveTimeZone() => TimeZones.Resolve(TimeZoneId);
}

/// <summary>Null alanlar değiştirilmez.</summary>
public sealed record ProfilePreferences(
    DiscoveryRadius? DiscoveryRadius = null,
    CostBand? Budget = null,
    int? WeeklyAvailableMinutes = null,
    IReadOnlyCollection<LifeCategory>? Goals = null,
    string? City = null,
    bool ClearCity = false,
    string? TimeZoneId = null,
    PhysicalEffort? MaxPhysicalEffort = null,
    NotificationPreference? Notifications = null);

public sealed record InterestSelection(Guid InterestId, double Weight);
