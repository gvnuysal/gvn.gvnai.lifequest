using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Notifications;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;
using Microsoft.Extensions.DependencyInjection;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RiskFollowUpTests(LifeQuestApiFactory factory)
{
    private static object Onboarding(string effort = "Vigorous", object[]? reactions = null) => new
    {
        goals = new[] { "Culture", "Fitness" },
        interests = new[] { new { code = "cinema", weight = 0.8 }, new { code = "art", weight = 0.6 } },
        weeklyAvailableMinutes = 300,
        budget = "Medium",
        discoveryRadius = "Explore",
        city = "İstanbul",
        timeZoneId = "Europe/Istanbul",
        maxPhysicalEffort = effort,
        starterReactions = reactions
    };

    [Fact]
    public async Task Starter_cards_are_diverse_and_reactions_seed_interest_weights()
    {
        var client = await factory.CreateUserClientAsync();

        var cards = await client.GetFromJsonAsync<JsonElement>("/api/v1/onboarding/starter-cards", Json);
        var list = cards.EnumerateArray().ToList();
        Assert.Equal(12, list.Count);
        Assert.Equal(6, list.Take(6).Select(c => c.GetProperty("category").GetString()).Distinct().Count());

        await CompleteOnboardingAsync(client, extra: Onboarding(reactions:
        [
            new { templateCode = "fitness-yoga-home", reaction = "Like" },
            new { templateCode = "culture-classic-film", reaction = "Dislike" },
            new { templateCode = "social-board-game-night", reaction = "Dislike" },
            new { templateCode = "unknown-code", reaction = "Like" }
        ]));

        var interests = (await client.GetFromJsonAsync<JsonElement>("/api/v1/profile", Json)).GetProperty("interests")
            .EnumerateArray().ToDictionary(i => i.GetProperty("code").GetString()!, i => i);

        Assert.Equal("Learned", interests["yoga"].GetProperty("source").GetString());
        Assert.Equal(0.45, interests["yoga"].GetProperty("weight").GetDouble(), 3);
        Assert.Equal(0.7, interests["cinema"].GetProperty("weight").GetDouble(), 3);   // 0.8 - 0.10
        Assert.False(interests.ContainsKey("board-games"));                           // "değil" yeni ilgi eklemez
    }

    [Fact]
    public async Task Effort_limit_is_respected_in_daily_and_contextual_offers()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client, extra: Onboarding(effort: "Light"));

        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var suggestions = await (await client.PostAsJsonAsync("/api/v1/quests/suggestions", new { availableMinutes = 180 }, Json))
            .Content.ReadFromJsonAsync<JsonElement>(Json);

        var efforts = today.GetProperty("quests").EnumerateArray()
            .Concat(suggestions.GetProperty("quests").EnumerateArray())
            .Select(q => q.GetProperty("effort").GetString())
            .ToList();

        Assert.NotEmpty(efforts);
        Assert.All(efforts, e => Assert.Contains(e, new[] { "None", "Light" }));
    }

    [Fact]
    public async Task Preferences_update_effort_and_notification_choice()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);

        var profile = await (await client.PatchAsJsonAsync("/api/v1/profile/preferences",
                new { maxPhysicalEffort = "Moderate", notificationPreference = "Off" }, Json))
            .Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal("Moderate", profile.GetProperty("maxPhysicalEffort").GetString());
        Assert.Equal("Off", profile.GetProperty("notificationPreference").GetString());
    }

    [Fact]
    public async Task Product_metrics_are_admin_only()
    {
        var user = await factory.CreateUserClientAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/v1/admin/metrics")).StatusCode);

        // Bootstrap listesindeki e-posta kayıt anında admin olur.
        var admin = await factory.CreateUserClientAsync(AdminEmail);
        await CompleteOnboardingAsync(user);
        var questId = (await user.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json))
            .GetProperty("quests")[0].GetProperty("id").GetGuid();
        await user.PostAsync($"/api/v1/quests/{questId}/accept", null);
        await user.PostAsync($"/api/v1/quests/{questId}/complete", null);

        var metrics = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/metrics?days=7", Json);

        Assert.True(metrics.GetProperty("activeUsers").GetInt32() >= 1);
        Assert.True(metrics.GetProperty("meaningfulCompletions").GetInt32() >= 1);
        Assert.True(metrics.GetProperty("northStar").GetDouble() > 0);
        Assert.True(metrics.GetProperty("funnel").GetProperty("offered").GetInt32() >= 3);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/admin/metrics?days=365")).StatusCode);
    }

    [Fact]
    public async Task Weekly_summary_is_generated_once_and_can_be_marked_read()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);
        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var questId = today.GetProperty("quests")[0].GetProperty("id").GetGuid();
        await client.PostAsync($"/api/v1/quests/{questId}/accept", null);
        await client.PostAsync($"/api/v1/quests/{questId}/complete", null);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/v1/profile", Json);
        var userId = profile.GetProperty("userId").GetGuid();

        // Bir hafta sonrasının pazartesisini simüle et: "geçen hafta" bu haftanın tamamlamalarını kapsar.
        var nextWeek = new ShiftedClock(TimeSpan.FromDays(7));
        async Task<bool> Generate()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var service = new WeeklySummaryService(
                sp.GetRequiredService<IUserProfileRepository>(), sp.GetRequiredService<IUserQuestRepository>(),
                sp.GetRequiredService<IWeeklySummaryRepository>(), sp.GetRequiredService<IUnitOfWork>(), nextWeek);
            return await service.GenerateForPreviousWeekAsync(userId, CancellationToken.None);
        }

        Assert.True(await Generate());
        Assert.False(await Generate());

        var latest = await client.GetFromJsonAsync<JsonElement>("/api/v1/summaries/latest", Json);
        Assert.Equal(1, latest.GetProperty("completedCount").GetInt32());
        Assert.Contains("1 gerçek deneyim", latest.GetProperty("message").GetString());

        var id = latest.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/summaries/{id}/read", null)).StatusCode);
        // Okunmamış özet kalmadığında gövdesiz 204 döner.
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/v1/summaries/latest")).StatusCode);
    }

    private sealed class ShiftedClock(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => base.GetUtcNow().Add(offset);
    }
}
