namespace LifeQuest.Mobile.Core.Api;

// Backend DTO'larının karşılıkları (web: core/api/models.ts). Enum'lar API'de metin olarak taşınır.

public enum LifeCategory { Explorer, Culture, Learning, Social, Fitness, Creativity }
public enum CostBand { Free, Low, Medium, High }
public enum DiscoveryRadius { Chill, Explore, SurpriseMe }
public enum QuestType { Daily, Weekly, Adventure, Epic }
public enum Difficulty { Easy, Medium, Hard, Heroic }
public enum QuestStatus { Offered, Accepted, Completed, Skipped, Expired }
public enum QuestSource { Daily, OnDemand, Saved }
public enum SkipReason { NotInterested, TooExpensive, NoTime, TooFar, NotToday, Other }
public enum FeedbackPreference { MoreLikeThis, LessLikeThis }
public enum InterestSource { Explicit, Learned }
public enum PhysicalEffort { None, Light, Moderate, Vigorous }
public enum NotificationPreference { Off, WeeklySummary }
public enum StarterReactionType { Like, Dislike }
public enum ErrorType { Failure, Validation, NotFound, Conflict, Unauthorized }
public enum PartyStatus { Open, Completed }
public enum OutdoorWeather { Unknown, Good, Poor }
public enum LocalPlaceKind { Venue, Event }
public enum IdeaStatus { Pending, Accepted, Rejected }

public sealed record ApiError(string Code, string Message, ErrorType Type);

// ── Oturum ──────────────────────────────────────────────────────────────────
public sealed record AuthSession(
    Guid UserId, string AccessToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt, string? RefreshToken);

public sealed record RegisterRequest(string Email, string Password, string DisplayName, int BirthYear, string? Language);

public sealed record LoginRequest(string Email, string Password);

// ── Katalog ve profil ───────────────────────────────────────────────────────
public sealed record Interest(Guid Id, string Code, string Name, LifeCategory Category);

public sealed record ProfileInterest(Guid Id, string Code, string Name, LifeCategory Category, double Weight, InterestSource Source);

public sealed record Profile(
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
    PhysicalEffort MaxPhysicalEffort,
    NotificationPreference NotificationPreference,
    int? DailyReminderHour,
    IReadOnlyList<ProfileInterest> Interests,
    string Language);

public sealed record InterestSelection(string Code, double? Weight);

public sealed record StarterReaction(string TemplateCode, StarterReactionType Reaction);

public sealed record OnboardingRequest(
    IReadOnlyList<LifeCategory> Goals,
    IReadOnlyList<InterestSelection> Interests,
    int WeeklyAvailableMinutes,
    CostBand Budget,
    DiscoveryRadius DiscoveryRadius,
    string? City,
    string? TimeZoneId,
    PhysicalEffort MaxPhysicalEffort,
    IReadOnlyList<StarterReaction> StarterReactions);

public sealed record StarterCard(
    string Code, string Title, string Description, LifeCategory Category, CostBand Cost, int MinMinutes, int MaxMinutes);

/// <summary>Yalnızca dolu alanlar gönderilir (PATCH).</summary>
public sealed record PreferencesRequest
{
    public DiscoveryRadius? DiscoveryRadius { get; init; }
    public CostBand? Budget { get; init; }
    public int? WeeklyAvailableMinutes { get; init; }
    public IReadOnlyList<LifeCategory>? Goals { get; init; }
    public string? City { get; init; }
    public bool? ClearCity { get; init; }
    public string? TimeZoneId { get; init; }
    public PhysicalEffort? MaxPhysicalEffort { get; init; }
    public NotificationPreference? NotificationPreference { get; init; }
    public int? DailyReminderHour { get; init; }
    public bool? ClearDailyReminder { get; init; }
    public string? Language { get; init; }
}

// ── Görev ───────────────────────────────────────────────────────────────────
public sealed record QuestReward(int LifeXp, int PrimaryCategoryXp, int SecondaryCategoryXp);

public sealed record Quest(
    Guid Id,
    string Title,
    string Description,
    QuestType Type,
    Difficulty Difficulty,
    LifeCategory Category,
    LifeCategory? SecondaryCategory,
    int MinMinutes,
    int MaxMinutes,
    CostBand Cost,
    PhysicalEffort Effort,
    QuestReward Reward,
    QuestStatus Status,
    QuestSource Source,
    DateTime OfferedAt,
    DateTime ExpiresAt,
    DateTime? AcceptedAt,
    DateTime? CompletedAt,
    SkipReason? SkipReason,
    int? Rating,
    FeedbackPreference? Preference,
    bool IsExploration,
    string Explanation,
    DateTime? PlannedAt);

public sealed record ScoreBreakdown(
    double Interest, double Novelty, double Context, double GoalFit, double Diversity, double FeedbackFit,
    double Repetition, double Friction, double Risk, double Total);

public sealed record PartyMember(string DisplayName, bool IsHost, bool IsYou, bool Completed, bool Dropped, int BonusXp);

public sealed record Party(
    string InviteCode, string QuestTitle, LifeCategory Category, PartyStatus Status, DateTime ExpiresAt, int MaxMembers,
    bool IsJoinable, IReadOnlyList<PartyMember> Members);

public sealed record PartyInvite(
    string InviteCode, string QuestTitle, LifeCategory Category, string HostName, int MemberCount, int MaxMembers,
    DateTime ExpiresAt, bool IsMember, bool IsJoinable, Guid? MyQuestId);

public sealed record NearbyPlace(
    Guid Id, LocalPlaceKind Kind, string Name, string? Address, string? Url, string? Note, DateTime? StartsAt, DateTime? EndsAt);

public sealed record QuestDetail(
    Quest Quest, ScoreBreakdown Score, IReadOnlyList<string> ReasonCodes, IReadOnlyList<NearbyPlace> NearbyPlaces, Party? Party);

public sealed record WeatherInfo(string City, double TemperatureC, string Summary, OutdoorWeather Outdoor, string? Advice, int Code);

public sealed record QuestList(DateOnly Date, IReadOnlyList<Quest> Quests, string? Message, WeatherInfo? Weather);

public sealed record Achievement(string Code, string Title, string Description, bool Unlocked, DateTime? UnlockedAt);

public sealed record QuestCompletion(
    Quest Quest, bool AlreadyCompleted, int LifeXp, int LifeLevel, bool LeveledUp, IReadOnlyList<Achievement> NewAchievements,
    int PartyBonusXp);

public sealed record QuestFeedbackResult(Quest Quest, IReadOnlyList<Achievement> NewAchievements);

public sealed record SuggestRequest(int? AvailableMinutes, CostBand? MaxCost);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages, bool HasPreviousPage, bool HasNextPage);

// ── İlerleme ────────────────────────────────────────────────────────────────
public sealed record CategoryProgress(LifeCategory Category, string DisplayName, int Xp, int Level, int NextLevelXp, int CompletedCount);

public sealed record XpEntry(DateTime At, string Description, int LifeXp, LifeCategory Category, int CategoryXp);

public sealed record Progress(
    int LifeXp, int LifeLevel, int CurrentLevelXp, int NextLevelXp, double LevelProgress, int TotalCompleted,
    IReadOnlyList<CategoryProgress> Categories, IReadOnlyList<XpEntry> RecentXp);

// ── Özet, kaydedilenler, fikirler ───────────────────────────────────────────
public sealed record WeeklySummary(
    Guid Id, DateOnly WeekStart, string Title, string Message, int CompletedCount, int XpEarned,
    IReadOnlyList<LifeCategory> NewCategories, LifeCategory? TopCategory, DateTime CreatedAt);

public sealed record SavedQuest(
    Guid TemplateId, string Title, string Description, LifeCategory Category, QuestType Type, int MinMinutes, int MaxMinutes,
    CostBand Cost, PhysicalEffort Effort, DateTime SavedAt, bool IsAvailable);

public sealed record IdeaRequest(string Title, string Description, LifeCategory Category, int Minutes, CostBand Cost, bool IsOutdoor);

public sealed record MyIdea(
    Guid Id, string Title, string Description, LifeCategory Category, int Minutes, CostBand Cost, bool IsOutdoor,
    IdeaStatus Status, string? ReviewNote, DateTime SubmittedAt, DateTime? ReviewedAt);

/// <summary>İndirilen dosya (veri dışa aktarma, .ics).</summary>
public sealed record DownloadedFile(string FileName, string ContentType, byte[] Content);
