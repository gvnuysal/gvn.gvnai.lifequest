using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class QuestFlowTests(LifeQuestApiFactory factory)
{
    [Fact]
    public async Task Daily_offers_require_onboarding_then_are_generated_once_per_day()
    {
        var client = await factory.CreateUserClientAsync();

        var beforeOnboarding = await client.GetAsync("/api/v1/quests/today");
        Assert.Equal(HttpStatusCode.BadRequest, beforeOnboarding.StatusCode);
        Assert.Contains("ONBOARDING_REQUIRED", await beforeOnboarding.Content.ReadAsStringAsync());

        await CompleteOnboardingAsync(client);

        var first = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var second = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);

        var firstIds = Ids(first);
        Assert.Equal(3, firstIds.Count);
        Assert.Equal(firstIds, Ids(second));
        Assert.All(first.GetProperty("quests").EnumerateArray(),
            q => Assert.False(string.IsNullOrWhiteSpace(q.GetProperty("explanation").GetString())));
    }

    [Fact]
    public async Task Completing_a_quest_grants_xp_exactly_once_and_updates_progress()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);
        var questId = Ids(await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json))[0];

        var notAccepted = await client.PostAsync($"/api/v1/quests/{questId}/complete", null);
        Assert.Equal(HttpStatusCode.Conflict, notAccepted.StatusCode);

        (await client.PostAsync($"/api/v1/quests/{questId}/accept", null)).EnsureSuccessStatusCode();

        var completion = await PostJson(client, $"/api/v1/quests/{questId}/complete");
        var again = await PostJson(client, $"/api/v1/quests/{questId}/complete");

        var xp = completion.GetProperty("quest").GetProperty("reward").GetProperty("lifeXp").GetInt32();
        Assert.False(completion.GetProperty("alreadyCompleted").GetBoolean());
        Assert.True(again.GetProperty("alreadyCompleted").GetBoolean());
        Assert.Contains(completion.GetProperty("newAchievements").EnumerateArray(),
            a => a.GetProperty("code").GetString() == "FIRST_STEP");

        var progress = await client.GetFromJsonAsync<JsonElement>("/api/v1/progress", Json);
        Assert.Equal(xp, progress.GetProperty("lifeXp").GetInt32());
        Assert.Equal(1, progress.GetProperty("totalCompleted").GetInt32());
        Assert.Single(progress.GetProperty("recentXp").EnumerateArray());
    }

    [Fact]
    public async Task Concurrent_completions_never_double_the_reward()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);
        var questId = Ids(await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json))[0];
        (await client.PostAsync($"/api/v1/quests/{questId}/accept", null)).EnsureSuccessStatusCode();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => client.PostAsync($"/api/v1/quests/{questId}/complete", null)));

        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict, r.StatusCode.ToString()));

        var progress = await client.GetFromJsonAsync<JsonElement>("/api/v1/progress", Json);
        Assert.Equal(1, progress.GetProperty("totalCompleted").GetInt32());
        Assert.Single(progress.GetProperty("recentXp").EnumerateArray());
    }

    [Fact]
    public async Task Not_interested_skip_lowers_interest_but_too_expensive_does_not()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);
        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var ids = Ids(today);

        var before = await InterestWeights(client);
        await client.PostAsJsonAsync($"/api/v1/quests/{ids[0]}/skip", new { reason = "TooExpensive" }, Json);
        Assert.Equal(before, await InterestWeights(client));

        await client.PostAsJsonAsync($"/api/v1/quests/{ids[1]}/skip", new { reason = "NotInterested" }, Json);
        var after = await InterestWeights(client);
        Assert.True(after.Sum(x => x.Value) <= before.Sum(x => x.Value));
    }

    [Fact]
    public async Task Contextual_suggestions_respect_available_time()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);

        var result = await PostJson(client, "/api/v1/quests/suggestions", new { availableMinutes = 60 });

        Assert.NotEmpty(result.GetProperty("quests").EnumerateArray());
        Assert.All(result.GetProperty("quests").EnumerateArray(),
            q => Assert.True(q.GetProperty("minMinutes").GetInt32() <= 60));
    }

    [Fact]
    public async Task Users_cannot_access_each_others_quests()
    {
        var owner = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(owner);
        var questId = Ids(await owner.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json))[0];

        var intruder = await factory.CreateUserClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/v1/quests/{questId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsync($"/api/v1/quests/{questId}/accept", null)).StatusCode);
    }

    [Fact]
    public async Task Invalid_requests_return_validation_errors_instead_of_500()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "short", displayName = "A", birthYear = 1990 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Contains(errors.EnumerateArray(), e => e.GetProperty("code").GetString() == "Email");
    }

    private static List<Guid> Ids(JsonElement list)
        => list.GetProperty("quests").EnumerateArray().Select(q => q.GetProperty("id").GetGuid()).ToList();

    private static async Task<JsonElement> PostJson(HttpClient client, string url, object? body = null)
    {
        var response = body is null ? await client.PostAsync(url, null) : await client.PostAsJsonAsync(url, body, Json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private static async Task<Dictionary<string, double>> InterestWeights(HttpClient client)
    {
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/v1/profile", Json);
        return profile.GetProperty("interests").EnumerateArray()
            .ToDictionary(i => i.GetProperty("code").GetString()!, i => i.GetProperty("weight").GetDouble());
    }
}
