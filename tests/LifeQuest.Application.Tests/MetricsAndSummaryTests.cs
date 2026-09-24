using LifeQuest.Application.Admin;
using LifeQuest.Application.Notifications;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Tests;

public sealed class ProductMetricsCalculatorTests
{
    [Fact]
    public void North_star_is_meaningful_experiences_per_active_user_per_week()
    {
        var snapshot = new ProductMetricsSnapshot(
            ActiveUsers: 4, Offered: 60, Accepted: 20, Completed: 12, MeaningfulCompletions: 10,
            NewCategoryCompletions: 3, ExplorationOffered: 10, ExplorationAccepted: 4, AverageRating: 4.25,
            SkipReasons: new Dictionary<SkipReason, int> { [SkipReason.NoTime] = 6, [SkipReason.NotInterested] = 2 },
            CompletionsByCategory: new Dictionary<LifeCategory, int> { [LifeCategory.Culture] = 12 });

        var weekly = ProductMetricsCalculator.Build(snapshot, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 7);
        var twoWeeks = ProductMetricsCalculator.Build(snapshot, DateTime.UtcNow.AddDays(-14), DateTime.UtcNow, 14);

        Assert.Equal(2.5, weekly.NorthStar);
        Assert.Equal(1.25, twoWeeks.NorthStar);
        Assert.Equal(0.333, weekly.Funnel.AcceptanceRate);
        Assert.Equal(0.6, weekly.Funnel.CompletionRate);
        Assert.Equal(0.25, weekly.NewCategoryDiscoveryRate);
        Assert.Equal(0.4, weekly.ExplorationAcceptanceRate);
        Assert.Equal(SkipReason.NoTime, weekly.SkipReasons[0].Key);
        Assert.Equal(0.75, weekly.SkipReasons[0].Share);
    }

    [Fact]
    public void Empty_period_has_zero_rates_not_errors()
    {
        var empty = new ProductMetricsSnapshot(0, 0, 0, 0, 0, 0, 0, 0, null,
            new Dictionary<SkipReason, int>(), new Dictionary<LifeCategory, int>());

        var dto = ProductMetricsCalculator.Build(empty, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 7);

        Assert.Equal(0, dto.NorthStar);
        Assert.Equal(0, dto.Funnel.AcceptanceRate);
        Assert.Null(dto.AverageRating);
    }
}

public sealed class WeeklySummaryWeekTests
{
    [Fact]
    public void Previous_week_is_monday_to_monday_in_user_local_time()
    {
        var istanbul = TimeZones.Resolve("Europe/Istanbul");
        // Pazartesi 28 Eylül 2026, 05:00 UTC = 08:00 İstanbul.
        var (weekStart, fromUtc, toUtc) = WeeklySummaryService.PreviousWeek(new DateTime(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc), istanbul);

        Assert.Equal(new DateOnly(2026, 9, 21), weekStart);
        Assert.Equal(new DateTime(2026, 9, 20, 21, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 9, 27, 21, 0, 0, DateTimeKind.Utc), toUtc);
    }
}
