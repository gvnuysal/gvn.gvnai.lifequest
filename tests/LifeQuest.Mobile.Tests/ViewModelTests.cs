using System.Net;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Localization;
using LifeQuest.Mobile.Core.Services;
using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Tests;

public sealed class ViewModelTests
{
    private const string Q1 = "11111111-1111-1111-1111-111111111111";
    private const string Q2 = "22222222-2222-2222-2222-222222222222";
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 9, 26, 9, 0, 0, TimeSpan.Zero));

    private static (ReminderService Reminders, FakeNotifications Notifications, FakePreferences Preferences) Reminders(FixedClock? clock = null)
    {
        var notifications = new FakeNotifications();
        var preferences = new FakePreferences();
        return (new ReminderService(notifications, preferences, clock ?? Clock), notifications, preferences);
    }

    [Fact]
    public async Task Today_splits_open_and_completed_quests()
    {
        using var _ = new LanguageScope(AppLanguage.En);
        var fake = new FakeApi()
            .On("GET quests/today", """{"date":"2026-09-26","quests":[""" + Json.Quest(Q1, "Offered") + "," + Json.Quest(Q2, "Completed")
                + """],"message":null,"weather":{"city":"Istanbul","temperatureC":21.6,"summary":"Clear","outdoor":"Good","advice":null,"code":0}}""")
            .On("GET quests/active", "[]")
            .On("GET progress", """{"lifeXp":120,"lifeLevel":2,"currentLevelXp":20,"nextLevelXp":150,"levelProgress":0.13,"totalCompleted":1,"categories":[],"recentXp":[]}""")
            .On("GET profile", Json.Profile)
            .On("GET summaries/latest", "", HttpStatusCode.NoContent)
            .On("GET saved", "[]");
        await fake.SignInAsync();
        var (reminders, _, _) = Reminders();
        var vm = new TodayViewModel(fake.Api, new ProfileState(fake.Api, new FakePreferences()), new FakeNavigator(), reminders, Clock);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Single(vm.OpenQuests);
        Assert.Single(vm.CompletedToday);
        Assert.False(vm.AllDone);
        Assert.Equal("Istanbul · 22° · Clear", vm.WeatherText);
        Assert.Equal("sun", vm.WeatherIcon);
        Assert.Equal("Lv. 2", vm.LevelShort);
        Assert.Equal("Good morning, Alex", vm.Greeting);
        Assert.False(vm.HasSummary);
    }

    [Fact]
    public async Task Onboarding_requires_goals_and_interests_and_sends_reactions()
    {
        using var _ = new LanguageScope(AppLanguage.En);
        string? sent = null;
        var fake = new FakeApi()
            .On("GET catalog/interests", """[{"id":"00000000-0000-0000-0000-000000000001","code":"coffee","name":"Coffee","category":"Explorer"}]""")
            .On("GET onboarding/starter-cards", """[{"code":"c1","title":"A","description":"d","category":"Culture","cost":"Free","minMinutes":10,"maxMinutes":20},{"code":"c2","title":"B","description":"d","category":"Social","cost":"Low","minMinutes":60,"maxMinutes":90}]""")
            .On("GET profile", Json.Profile.Replace("\"onboardingCompleted\":true", "\"onboardingCompleted\":false"))
            .On("PUT profile/onboarding", body => { sent = body; return FakeTransport.JsonResponse(HttpStatusCode.OK, Json.Profile); });
        await fake.SignInAsync();
        var navigator = new FakeNavigator();
        var profile = new ProfileState(fake.Api, new FakePreferences());
        var flow = new AppFlow(fake.Session, profile, navigator, new FakeToast(), new FakePreferences());
        var vm = new OnboardingViewModel(fake.Api, profile, flow, new FakeToast(), new FakeAppInfo());

        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Equal("Hi Alex! Which areas do you want to grow in?", vm.Greeting);
        Assert.False(vm.CanContinue);
        vm.ToggleGoalCommand.Execute(vm.Goals[1]);
        Assert.True(vm.CanContinue);
        await vm.NextCommand.ExecuteAsync(null);

        Assert.False(vm.CanContinue);
        var coffee = vm.InterestGroups[0][0];
        vm.CycleInterestCommand.Execute(coffee);
        vm.CycleInterestCommand.Execute(coffee);
        Assert.True(coffee.IsLoved);
        Assert.True(vm.CanContinue);
        await vm.NextCommand.ExecuteAsync(null);

        Assert.Equal("A", vm.CurrentCard!.Title);
        vm.LikeCommand.Execute(null);
        Assert.Equal("1–1.5 h", vm.CardDuration);
        vm.SkipCardCommand.Execute(null);
        Assert.True(vm.CardsDone);
        Assert.Equal("You liked 1 card. Your first suggestions will build on that.", vm.LikedText);

        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);
        Assert.True(vm.IsLastStep);
        vm.City = "  Istanbul ";
        await vm.NextCommand.ExecuteAsync(null);

        Assert.NotNull(sent);
        Assert.Contains("\"goals\":[\"Culture\"]", sent);
        Assert.Contains("{\"code\":\"coffee\",\"weight\":0.9}", sent);
        Assert.Contains("\"starterReactions\":[{\"templateCode\":\"c1\",\"reaction\":\"Like\"}]", sent);
        Assert.Contains("\"city\":\"Istanbul\"", sent);
        Assert.Contains("\"timeZoneId\":\"Europe/Istanbul\"", sent);
        Assert.Equal([AppRoot.Main], navigator.Roots);
    }

    [Fact]
    public async Task Quest_actions_show_feedback_and_reload_after_a_conflict()
    {
        using var _ = new LanguageScope(AppLanguage.En);
        var fake = new FakeApi()
            .On($"GET quests/{Q1}", Json.Detail(Json.Quest(Q1, "Offered")))
            .On($"POST quests/{Q1}/accept", Json.Quest(Q1, "Accepted"))
            .On($"POST quests/{Q1}/complete", """[{"code":"QUEST_EXPIRED","message":"This quest has expired.","type":"Conflict"}]""", HttpStatusCode.Conflict);
        await fake.SignInAsync();
        var toast = new FakeToast();
        var (reminders, _, _) = Reminders();
        var vm = new QuestViewModel(fake.Api, new FakeNavigator(), toast, new FakeShare(), new FakeAppInfo(), reminders, Clock) { Id = Guid.Parse(Q1) };

        await vm.LoadCommand.ExecuteAsync(null);
        Assert.True(vm.IsOffered);
        Assert.Equal(5, vm.Facts.Count);
        Assert.Equal(9, vm.ScoreBars.Count);
        Assert.Equal(1, vm.ScoreBars[2].Ratio);
        Assert.Equal("−20", vm.ScoreBars[6].Value);

        await vm.AcceptCommand.ExecuteAsync(null);
        Assert.True(vm.IsAccepted);
        Assert.Equal(("success", "Quest accepted. Enjoy!"), toast.Messages.Last());
        Assert.True(vm.CanCreateParty);

        await vm.CompleteCommand.ExecuteAsync(null);
        Assert.Equal(("error", "This quest has expired."), toast.Messages.Last());
        Assert.Equal(2, fake.Calls.Count(c => c.Key == $"GET quests/{Q1}"));
        Assert.False(vm.IsCelebrating);
    }

    [Fact]
    public async Task Completing_opens_the_celebration_and_skips_todays_reminder()
    {
        using var _ = new LanguageScope(AppLanguage.En);
        var completed = Json.Quest(Q1, "Completed");
        var fake = new FakeApi()
            .On($"GET quests/{Q1}", Json.Detail(Json.Quest(Q1, "Accepted")))
            .On($"POST quests/{Q1}/complete", $$"""{"quest":{{completed}},"alreadyCompleted":false,"lifeXp":158,"lifeLevel":2,"leveledUp":true,"newAchievements":[{"code":"first","title":"First Step","description":"d","unlocked":true,"unlockedAt":null}],"partyBonusXp":0}""")
            .On($"POST quests/{Q1}/feedback", $$"""{"quest":{{completed}},"newAchievements":[]}""");
        await fake.SignInAsync();
        var toast = new FakeToast();
        var (reminders, notifications, preferences) = Reminders();
        preferences.Set(ReminderService.HourKey, "19");
        var vm = new QuestViewModel(fake.Api, new FakeNavigator(), toast, new FakeShare(), new FakeAppInfo(), reminders, Clock) { Id = Guid.Parse(Q1) };
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.CompleteCommand.ExecuteAsync(null);

        var celebration = Assert.IsType<CelebrationViewModel>(vm.Celebration);
        Assert.Equal("You reached level 2!", celebration.LevelUp);
        celebration.Animate(1);
        Assert.Equal(38, celebration.ShownXp);
        Assert.DoesNotContain(notifications.Scheduled, n => n.At.Date == Clock.Now.Date);
        Assert.Equal(6, notifications.Scheduled.Count);

        celebration.Rating = 5;
        celebration.ToggleMoreCommand.Execute(null);
        await celebration.SubmitCommand.ExecuteAsync(null);
        Assert.Contains(fake.Calls, c => c.Key == $"POST quests/{Q1}/feedback" && c.Body == """{"rating":5,"preference":"MoreLikeThis"}""");
        Assert.Null(vm.Celebration);
        Assert.Equal(("success", "Thanks! Your suggestions will improve based on this."), toast.Messages.Last());
    }

    [Fact]
    public async Task Reminders_cover_the_next_seven_days_at_the_chosen_hour()
    {
        var (reminders, notifications, preferences) = Reminders();

        Assert.True(await reminders.EnableAsync(8, completedToday: false));

        Assert.Equal("8", preferences.Values[ReminderService.HourKey]);
        // 09:00'da açıldı: bugünün 08:00'i geçti, yarından itibaren 6 gün.
        Assert.Equal(6, notifications.Scheduled.Count);
        Assert.All(notifications.Scheduled, n => Assert.Equal(8, n.At.Hour));

        await reminders.RescheduleAsync(completedToday: false);
        Assert.Equal(6, notifications.Scheduled.Count);

        await reminders.DisableAsync();
        Assert.Empty(notifications.Scheduled);
        Assert.Null(reminders.Hour);
    }

    [Fact]
    public async Task Denied_permission_keeps_the_reminder_off()
    {
        var (reminders, notifications, preferences) = Reminders();
        notifications.Allow = false;

        Assert.False(await reminders.EnableAsync(19, completedToday: false));
        Assert.Empty(preferences.Values);
        Assert.Empty(notifications.Scheduled);
    }

    [Fact]
    public async Task Party_links_open_after_sign_in()
    {
        var fake = new FakeApi().On("GET profile", Json.Profile);
        var navigator = new FakeNavigator();
        var profile = new ProfileState(fake.Api, new FakePreferences());
        var flow = new AppFlow(fake.Session, profile, navigator, new FakeToast(), new FakePreferences());

        await flow.OpenLinkAsync(new Uri("lifequest://party/AB12CD"));
        Assert.Empty(navigator.Visited);

        await fake.SignInAsync();
        await flow.EnterAsync();

        Assert.Equal([AppRoot.Main], navigator.Roots);
        Assert.Equal(Routes.Party, Assert.Single(navigator.Visited));
        Assert.Equal("AB12CD", navigator.LastParameters!["code"]);

        await flow.OpenLinkAsync(new Uri("https://lifequest.test/party/ZZ99"));
        Assert.Equal("ZZ99", navigator.LastParameters!["code"]);
    }

    [Fact]
    public async Task Ending_the_session_returns_to_login_with_a_message()
    {
        using var _ = new LanguageScope(AppLanguage.En);
        var fake = new FakeApi().On("POST auth/logout", "", HttpStatusCode.OK);
        await fake.SignInAsync();
        var navigator = new FakeNavigator();
        var toast = new FakeToast();
        var flow = new AppFlow(fake.Session, new ProfileState(fake.Api, new FakePreferences()), navigator, toast, new FakePreferences());

        await fake.Session.EndAsync(Core.Auth.SessionEndReason.Suspended);

        Assert.Equal([AppRoot.Login], navigator.Roots);
        Assert.Equal("error", toast.Messages.Single().Kind);
    }

    [Fact]
    public async Task Register_maps_server_field_errors()
    {
        using var _ = new LanguageScope(AppLanguage.En);
        var fake = new FakeApi().On("POST auth/register", """[{"code":"EMAIL_TAKEN","message":"This email is already registered.","type":"Conflict"}]""", HttpStatusCode.Conflict);
        var flow = new AppFlow(fake.Session, new ProfileState(fake.Api, new FakePreferences()), new FakeNavigator(), new FakeToast(), new FakePreferences());
        var vm = new RegisterViewModel(fake.Auth, flow, new FakeNavigator(), Clock)
        {
            DisplayName = "A",
            Email = "bad",
            Password = "short"
        };

        await vm.RegisterCommand.ExecuteAsync(null);
        Assert.Equal("Must be at least 2 characters.", vm.DisplayNameError);
        Assert.Equal("Enter a valid email.", vm.EmailError);
        Assert.NotNull(vm.PasswordError);
        Assert.Empty(fake.Calls);

        vm.DisplayName = "Alex";
        vm.Email = "alex@example.com";
        vm.Password = "Passw0rd!";
        await vm.RegisterCommand.ExecuteAsync(null);
        Assert.Null(vm.EmailError);
        Assert.Equal("This email is already registered.", vm.Error);
        Assert.Contains("\"language\":\"en\"", fake.Calls.Single().Body);
    }
}
