using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using LifeQuest.Application.Profiles;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Admin;

/// <summary>
/// KVKK/GDPR veri taşınabilirliği. Kullanıcıya ait her kayıt açık projeksiyonla yazılır: şifre özeti, oturum
/// token'ları ve denetim kayıtları gibi güvenlik verileri dışarıda kalır; başka kullanıcıya ait veri sorgulanmaz.
/// </summary>
internal sealed class UserDataExporter(LifeQuestDbContext db) : IUserDataExporter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<byte[]?> ExportAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var account = await db.UserAccounts.AsNoTracking()
            .Where(a => a.Id == userId)
            .Select(a => new { a.Id, a.Email, a.DisplayName, a.BirthYear, a.Role, a.CreatedAt, a.LastLoginAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (account is null)
            return null;

        var interestNames = await db.Interests.AsNoTracking().ToDictionaryAsync(i => i.Id, i => i.Name, cancellationToken);

        var profile = await db.UserProfiles.AsNoTracking().Include(p => p.Interests)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var progress = await db.PlayerProgress.AsNoTracking().Include(p => p.Categories).Include(p => p.Achievements)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var quests = await db.UserQuests.AsNoTracking().Where(q => q.UserId == userId).OrderBy(q => q.OfferedAt)
            .ToListAsync(cancellationToken);
        var xp = await db.XpTransactions.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var summaries = await db.WeeklySummaries.AsNoTracking().Where(s => s.UserId == userId).OrderBy(s => s.WeekStart)
            .ToListAsync(cancellationToken);
        var saved = await db.SavedQuests.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        var ideas = await db.QuestIdeas.AsNoTracking().Where(i => i.UserId == userId).ToListAsync(cancellationToken);

        var export = new
        {
            exportedAt = nowUtc,
            format = "LifeQuest veri dışa aktarımı v1",
            note = "Şifre özeti ve oturum anahtarları güvenlik nedeniyle dahil edilmez.",
            account,
            profile = profile is null ? null : new
            {
                profile.OnboardingCompleted,
                profile.DiscoveryRadius,
                profile.Budget,
                profile.WeeklyAvailableMinutes,
                profile.Goals,
                profile.City,
                profile.TimeZoneId,
                profile.MaxPhysicalEffort,
                profile.NotificationPreference,
                interests = profile.Interests.Select(i => new
                {
                    name = interestNames.GetValueOrDefault(i.InterestId, i.InterestId.ToString()),
                    i.Weight,
                    i.Source,
                    i.UpdatedAt
                })
            },
            progress = progress is null ? null : new
            {
                progress.LifeXp,
                progress.LifeLevel,
                progress.TotalCompleted,
                categories = progress.Categories.Select(c => new { c.Category, c.Xp, c.Level, c.CompletedCount }),
                achievements = progress.Achievements.Select(a => new { a.Code, a.UnlockedAt })
            },
            quests = quests.Select(q => new
            {
                q.Id, q.Title, q.Description, q.Type, q.Difficulty, q.Category, q.SecondaryCategory, q.MinMinutes, q.MaxMinutes,
                q.Cost, q.Effort, reward = new { q.Reward.LifeXp, q.Reward.PrimaryCategoryXp, q.Reward.SecondaryCategoryXp },
                q.Source, q.Status, q.OfferedAt, q.ExpiresAt, q.AcceptedAt, q.CompletedAt, q.SkippedAt, q.ExpiredAt, q.SkipReason,
                q.PlannedAt, q.Rating, q.Preference, q.FeedbackAt, q.IsExploration, q.Explanation, q.ReasonCodes,
                score = q.Score
            }),
            xpTransactions = xp.Select(x => new
            {
                x.CreatedAt, x.Description, x.LifeXp, x.PrimaryCategory, x.PrimaryCategoryXp, x.SecondaryCategory, x.SecondaryCategoryXp
            }),
            weeklySummaries = summaries.Select(s => new
            {
                s.WeekStart, s.CompletedCount, s.XpEarned, s.NewCategories, s.TopCategory, s.Title, s.Message, s.ReadAt
            }),
            savedForLater = saved.Select(s => new { s.TemplateId, s.SavedAt }),
            ideas = ideas.Select(i => new
            {
                i.Title, i.Description, i.Category, i.Minutes, i.Cost, i.IsOutdoor, i.Status, i.ReviewNote, i.SubmittedAt, i.ReviewedAt
            })
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, Json);
    }
}
