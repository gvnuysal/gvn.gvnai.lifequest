using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class LocalizationApiTests(LifeQuestApiFactory factory)
{
    private HttpClient Client(string? language)
    {
        var client = factory.CreateClient();
        if (language is not null) client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        return client;
    }

    [Fact]
    public async Task Error_messages_follow_accept_language_and_default_to_turkish()
    {
        var body = new { email = "nobody@example.com", password = "WrongPass1" };
        var english = await (await Client("en-US,en;q=0.9").PostAsJsonAsync("/api/v1/auth/login", body)).Content.ReadAsStringAsync();
        var turkish = await (await Client(null).PostAsJsonAsync("/api/v1/auth/login", body)).Content.ReadAsStringAsync();

        Assert.Contains("Email or password is incorrect.", english);
        Assert.Contains("E-posta veya şifre hatalı.", turkish);
        Assert.Contains("INVALID_CREDENTIALS", english);
    }

    [Fact]
    public async Task Validation_messages_are_localized_too()
    {
        var response = await Client("en").PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "abcdefgh", displayName = "X", birthYear = 1990 });
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("The password must contain at least one letter and one digit.", text);
        Assert.DoesNotContain("Şifre", text);
    }

    [Fact]
    public async Task Account_language_comes_from_the_request_and_can_be_changed()
    {
        var client = Client("en");
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"lang-{Guid.NewGuid():N}@example.com", password = Password, displayName = "Lang", birthYear = 1990 });
        Authorize(client, await register.Content.ReadFromJsonAsync<JsonElement>(Json));

        Assert.Equal("en", (await client.GetFromJsonAsync<JsonElement>("/api/v1/profile", Json)).GetProperty("language").GetString());

        var changed = await client.PatchAsJsonAsync("/api/v1/profile/preferences", new { language = "tr" }, Json);
        Assert.Equal("tr", (await changed.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("language").GetString());

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PatchAsJsonAsync("/api/v1/profile/preferences", new { language = "de" }, Json)).StatusCode);
    }

    private async Task<HttpClient> EnglishUserAsync()
    {
        var client = Client("en");
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"en-{Guid.NewGuid():N}@example.com", password = Password, displayName = "Alex", birthYear = 1990 });
        register.EnsureSuccessStatusCode();
        Authorize(client, await register.Content.ReadFromJsonAsync<JsonElement>(Json));
        await CompleteOnboardingAsync(client);
        return client;
    }

    [Fact]
    public async Task Quests_interests_and_achievements_come_back_in_english_and_switch_with_the_header()
    {
        var client = await EnglishUserAsync();
        var english = LifeQuest.Infrastructure.Persistence.Seed.CatalogSeedTranslations.Templates.Values.Select(t => t.Title).ToHashSet();

        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var quests = today.GetProperty("quests").EnumerateArray().ToList();
        Assert.All(quests, q =>
        {
            Assert.Contains(q.GetProperty("title").GetString()!, english);
            Assert.Matches("^(Suggested because|Since|One of the best)", q.GetProperty("explanation").GetString());
        });

        var interests = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/interests", Json);
        Assert.Contains(interests.EnumerateArray(), i => i.GetProperty("name").GetString() == "Café Culture");

        // Aynı görev Türkçe istekte Türkçe döner: iki dil de saklanır.
        var id = quests[0].GetProperty("id").GetGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/quests/{id}");
        request.Headers.AcceptLanguage.ParseAdd("tr");
        var turkish = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.DoesNotContain(turkish.GetProperty("quest").GetProperty("title").GetString()!, english);

        await client.PostAsync($"/api/v1/quests/{id}/accept", null);
        var completion = await (await client.PostAsync($"/api/v1/quests/{id}/complete", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Contains(completion.GetProperty("newAchievements").EnumerateArray(), a => a.GetProperty("title").GetString() == "First Step");

        var xp = await client.GetFromJsonAsync<JsonElement>("/api/v1/progress", Json);
        Assert.Contains(xp.GetProperty("categories").EnumerateArray(), c => c.GetProperty("displayName").GetString() == "Movement");
    }

    [Fact]
    public async Task Catalog_sync_backfills_english_for_quests_given_before_localization()
    {
        var client = await EnglishUserAsync();
        var english = LifeQuest.Infrastructure.Persistence.Seed.CatalogSeedTranslations.Templates.Values.Select(t => t.Title).ToHashSet();
        var ids = (await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json))
            .GetProperty("quests").EnumerateArray().Select(q => q.GetProperty("id").GetGuid()).ToList();

        // Yerelleştirme öncesi kopya: yalnızca Türkçe.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeQuest.Infrastructure.Persistence.LifeQuestDbContext>();
            await db.UserQuests.Where(q => ids.Contains(q.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.TitleEn, (string?)null).SetProperty(q => q.DescriptionEn, (string?)null));
        }

        var before = await client.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{ids[0]}", Json);
        Assert.DoesNotContain(before.GetProperty("quest").GetProperty("title").GetString()!, english);

        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<LifeQuest.Infrastructure.Persistence.Seed.CatalogSeeder>().SeedAsync();

        var after = await client.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{ids[0]}", Json);
        Assert.Contains(after.GetProperty("quest").GetProperty("title").GetString()!, english);
    }

    [Fact]
    public async Task Background_push_uses_the_account_language()
    {
        // Saat sınırında başlamamak için önce beklenir; yerel saat bekledikten sonra hesaplanır.
        if (DateTime.UtcNow is { Minute: >= 58 } now)
            await Task.Delay(TimeSpan.FromMinutes(60 - now.Minute) - TimeSpan.FromSeconds(now.Second) + TimeSpan.FromSeconds(2));
        var (timeZoneId, localHour) = Enumerable.Range(-12, 27)
            .Select(offset => ($"Etc/GMT{(offset <= 0 ? "+" : "-")}{Math.Abs(offset)}", offset))
            .Select(z => (z.Item1, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(z.Item1)).Hour))
            .First(z => z.Hour is >= 8 and <= 21);

        var client = await EnglishUserAsync();
        var endpoint = $"https://push.example.com/en/{Guid.NewGuid():N}";
        await client.PatchAsJsonAsync("/api/v1/profile/preferences", new { timeZoneId, dailyReminderHour = localHour }, Json);
        await client.PutAsJsonAsync("/api/v1/push/subscription", new
        {
            endpoint,
            keys = new { p256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM", auth = "tBHItJI5svbpez7KI4CCXg" }
        });

        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<LifeQuest.Application.Notifications.DailyReminderService>()
                .SendDueAsync(CancellationToken.None);

        var push = Assert.Single(factory.Push.Sent, s => s.Endpoint == endpoint);
        Assert.Contains(push.Notification.Title, new[] { "A step today?", "Today's quests are ready" });
    }
}
