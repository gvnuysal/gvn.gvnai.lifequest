using Gvn.GvnFramework.Core.Results;
using LifeQuest.Domain.Localization;
using LifeQuest.Domain.Notifications;

namespace LifeQuest.Domain.Profiles;

public static class ProfileErrors
{
    public static Error ProfileNotFound =>
        Error.NotFound("PROFILE_NOT_FOUND", Text.Of("Profil bulunamadı.", "Profile not found."));

    public static Error OnboardingRequired =>
        Error.Validation("ONBOARDING_REQUIRED", Text.Of(
            "Öneri alabilmek için önce onboarding'i tamamlamalısın.",
            "Finish onboarding to get suggestions."));

    public static Error InterestsRequired =>
        Error.Validation("INTERESTS_REQUIRED", Text.Of("En az bir ilgi alanı seçmelisin.", "Pick at least one interest."));

    public static Error InvalidWeeklyMinutes =>
        Error.Validation("INVALID_WEEKLY_MINUTES", Text.Of(
            $"Haftalık süre {UserProfile.MinWeeklyMinutes} ile {UserProfile.MaxWeeklyMinutes} dakika arasında olmalı.",
            $"Weekly time must be between {UserProfile.MinWeeklyMinutes} and {UserProfile.MaxWeeklyMinutes} minutes."));

    public static Error InvalidTimeZone =>
        Error.Validation("INVALID_TIME_ZONE", Text.Of(
            "Geçersiz saat dilimi. IANA formatı kullanın (ör. Europe/Istanbul).",
            "Invalid time zone. Use the IANA format (e.g. Europe/Istanbul)."));

    public static Error InvalidReminderHour =>
        Error.Validation("INVALID_REMINDER_HOUR", Text.Of(
            $"Hatırlatma saati {DailyReminder.EarliestHour}:00 ile {DailyReminder.LatestHour}:00 arasında olmalı.",
            $"The reminder time must be between {DailyReminder.EarliestHour}:00 and {DailyReminder.LatestHour}:00."));

    public static Error UnknownInterests(IEnumerable<string> codes) =>
        Error.Validation("UNKNOWN_INTERESTS", Text.Of(
            $"Bilinmeyen ilgi alanları: {string.Join(", ", codes)}",
            $"Unknown interests: {string.Join(", ", codes)}"));
}
