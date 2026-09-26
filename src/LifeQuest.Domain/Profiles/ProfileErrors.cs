using Gvn.GvnFramework.Core.Results;

namespace LifeQuest.Domain.Profiles;

public static class ProfileErrors
{
    public static readonly Error ProfileNotFound =
        Error.NotFound("PROFILE_NOT_FOUND", "Profil bulunamadı.");

    public static readonly Error OnboardingRequired =
        Error.Validation("ONBOARDING_REQUIRED", "Öneri alabilmek için önce onboarding'i tamamlamalısın.");

    public static readonly Error InterestsRequired =
        Error.Validation("INTERESTS_REQUIRED", "En az bir ilgi alanı seçmelisin.");

    public static readonly Error InvalidWeeklyMinutes =
        Error.Validation("INVALID_WEEKLY_MINUTES",
            $"Haftalık süre {UserProfile.MinWeeklyMinutes} ile {UserProfile.MaxWeeklyMinutes} dakika arasında olmalı.");

    public static readonly Error InvalidTimeZone =
        Error.Validation("INVALID_TIME_ZONE", "Geçersiz saat dilimi. IANA formatı kullanın (ör. Europe/Istanbul).");

    public static readonly Error InvalidReminderHour =
        Error.Validation("INVALID_REMINDER_HOUR",
            $"Hatırlatma saati {Notifications.DailyReminder.EarliestHour}:00 ile {Notifications.DailyReminder.LatestHour}:00 arasında olmalı.");

    public static Error UnknownInterests(IEnumerable<string> codes) =>
        Error.Validation("UNKNOWN_INTERESTS", $"Bilinmeyen ilgi alanları: {string.Join(", ", codes)}");
}
