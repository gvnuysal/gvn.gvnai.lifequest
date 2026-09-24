using Gvn.GvnFramework.Domain.Entities;
using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Notifications;

/// <summary>Uygulama içi haftalık özet. (UserId, WeekStart) benzersizdir; job tekrar çalışsa da çift özet oluşmaz.</summary>
public sealed class WeeklySummary : Entity
{
    public Guid UserId { get; private set; }

    /// <summary>Özetlenen haftanın (kullanıcının yerel takvimine göre) pazartesi günü.</summary>
    public DateOnly WeekStart { get; private set; }

    public int CompletedCount { get; private set; }
    public int XpEarned { get; private set; }
    public List<LifeCategory> NewCategories { get; private set; } = [];
    public LifeCategory? TopCategory { get; private set; }
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private WeeklySummary() { }

    public static WeeklySummary Create(Guid userId, DateOnly weekStart, WeeklyStats stats, DateTime nowUtc)
    {
        var (title, message) = WeeklySummaryComposer.Compose(stats);
        return new WeeklySummary
        {
            UserId = userId,
            WeekStart = weekStart,
            CompletedCount = stats.CompletedCount,
            XpEarned = stats.XpEarned,
            NewCategories = stats.NewCategories.ToList(),
            TopCategory = stats.TopCategory,
            Title = title,
            Message = message,
            CreatedAt = nowUtc
        };
    }

    public void MarkRead(DateTime nowUtc) => ReadAt ??= nowUtc;
}

public sealed record WeeklyStats(
    int CompletedCount,
    int XpEarned,
    IReadOnlyList<LifeCategory> NewCategories,
    LifeCategory? TopCategory,
    int OpenAcceptedCount);
