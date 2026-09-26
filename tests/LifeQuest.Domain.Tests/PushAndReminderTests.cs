using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;

namespace LifeQuest.Domain.Tests;

public sealed class DailyReminderTests
{
    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(22, true)]
    [InlineData(23, false)]
    public void Night_hours_cannot_be_chosen(int hour, bool valid)
    {
        Assert.Equal(valid, DailyReminder.IsValidHour(hour));
        var profile = UserProfile.CreateFor(Guid.NewGuid());
        Assert.Equal(valid, profile.UpdatePreferences(new ProfilePreferences(DailyReminderHour: hour)).Succeeded);
    }

    [Fact]
    public void Reminder_is_due_once_per_local_day_within_the_chosen_hour()
    {
        var profile = UserProfile.CreateFor(Guid.NewGuid());
        profile.UpdatePreferences(new ProfilePreferences(TimeZoneId: "Europe/Istanbul", DailyReminderHour: 9));

        var nineLocal = new DateTime(2026, 10, 5, 6, 10, 0, DateTimeKind.Utc); // İstanbul 09:10
        Assert.False(profile.IsDailyReminderDue(nineLocal.AddHours(-1)));
        Assert.True(profile.IsDailyReminderDue(nineLocal));

        profile.MarkDailyReminderHandled(nineLocal);
        Assert.False(profile.IsDailyReminderDue(nineLocal.AddMinutes(30)));
        Assert.True(profile.IsDailyReminderDue(nineLocal.AddDays(1)));

        profile.UpdatePreferences(new ProfilePreferences(ClearDailyReminder: true));
        Assert.Null(profile.DailyReminderHour);
        Assert.False(profile.IsDailyReminderDue(nineLocal.AddDays(2)));
    }

    [Theory]
    [InlineData(2, 0, "2 aktif görevin var")]
    [InlineData(1, 3, "Aktif bir görevin var")]
    [InlineData(0, 3, "3 öneri")]
    public void Message_nudges_towards_what_is_already_waiting(int active, int offers, string expected)
        => Assert.Contains(expected, DailyReminder.Compose(active, offers).Body);
}

public sealed class PushSubscriptionTests
{
    [Fact]
    public void Repeated_failures_drop_the_device_and_a_success_resets_the_count()
    {
        var subscription = PushSubscription.Create(Guid.NewGuid(), "https://push.example.com/x", "key", "auth", DateTime.UtcNow);
        for (var i = 1; i < PushSubscription.MaxConsecutiveFailures; i++)
            Assert.False(subscription.RecordFailure());

        subscription.RecordSuccess(DateTime.UtcNow);
        Assert.Equal(0, subscription.ConsecutiveFailures);

        for (var i = 1; i < PushSubscription.MaxConsecutiveFailures; i++)
            subscription.RecordFailure();
        Assert.True(subscription.RecordFailure());
    }
}
