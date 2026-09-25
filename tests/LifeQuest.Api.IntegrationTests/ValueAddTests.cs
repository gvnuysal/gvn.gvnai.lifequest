using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LifeQuest.Domain.Experiments;
using LifeQuest.Domain.Quests;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ValueAddTests(LifeQuestApiFactory factory)
{
    [Theory]
    [InlineData("GET", "/api/v1/admin/experiments")]
    [InlineData("POST", "/api/v1/admin/experiments/00000000-0000-0000-0000-000000000001/start")]
    [InlineData("GET", "/api/v1/admin/ideas")]
    [InlineData("POST", "/api/v1/admin/ideas/00000000-0000-0000-0000-000000000001/reject")]
    public async Task Regular_users_cannot_reach_new_admin_endpoints(string method, string url)
    {
        var client = await factory.CreateUserClientAsync();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { note = "x" })
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Sonra yaparım, planlama, takvim ──────────────────────────────────────

    [Fact]
    public async Task Saved_quest_can_be_started_later_planned_and_exported_to_a_calendar()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);
        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var offered = today.GetProperty("quests")[0];
        var questId = offered.GetProperty("id").GetGuid();

        await AssertErrorAsync(await client.PutAsJsonAsync($"/api/v1/quests/{questId}/plan",
            new { plannedAtLocal = DateTime.Today.AddDays(1).AddHours(10) }), HttpStatusCode.Conflict, "PLAN_REQUIRES_ACCEPTED");

        var saved = await (await client.PostAsync($"/api/v1/quests/{questId}/save", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        var templateId = saved.GetProperty("templateId").GetGuid();
        Assert.True(saved.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/quests/{questId}/save", null)).StatusCode); // tekrar çağrılabilir
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/v1/saved", Json)).EnumerateArray());

        // Aynı template bugünün önerilerinde hâlâ açık: motor ikinci kez sunmaz.
        await AssertErrorAsync(await client.PostAsync($"/api/v1/saved/{templateId}/start", null), HttpStatusCode.Conflict, "NOT_OFFERABLE_NOW");

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/quests/{questId}/skip", new { reason = "NotToday" }, Json)).StatusCode);
        var started = await client.PostAsync($"/api/v1/saved/{templateId}/start", null);
        var quest = await started.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal("Accepted", quest.GetProperty("status").GetString());
        Assert.Equal("Saved", quest.GetProperty("source").GetString());
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/v1/saved", Json)).EnumerateArray());
        await AssertErrorAsync(await client.PostAsync($"/api/v1/saved/{templateId}/start", null), HttpStatusCode.NotFound, "SAVED_NOT_FOUND");

        var startedId = quest.GetProperty("id").GetGuid();
        await AssertErrorAsync(await client.GetAsync($"/api/v1/quests/{startedId}/calendar.ics"), HttpStatusCode.Conflict, "QUEST_NOT_PLANNED");

        var planned = await client.PutAsJsonAsync($"/api/v1/quests/{startedId}/plan",
            new { plannedAtLocal = DateTime.UtcNow.AddHours(3).AddDays(1).Date.AddHours(10) });
        Assert.Equal(HttpStatusCode.OK, planned.StatusCode);
        Assert.NotEqual(JsonValueKind.Null, (await planned.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("plannedAt").ValueKind);

        var ics = await client.GetAsync($"/api/v1/quests/{startedId}/calendar.ics");
        Assert.Equal("text/calendar", ics.Content.Headers.ContentType?.MediaType);
        var content = await ics.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VEVENT", content);
        Assert.Contains("DTSTART:", content);
        Assert.Contains("T070000Z", content); // İstanbul 10:00 = 07:00 UTC
    }

    // ── A/B deneyi ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Experiment_assigns_users_records_variants_and_the_winner_can_be_adopted()
    {
        var admin = await factory.CreateAdminClientAsync();
        var created = await admin.PostAsJsonAsync("/api/v1/admin/experiments", new
        {
            name = "Çeşitlilik 0,3",
            hypothesis = "Daha çeşitli günlük liste yeni kategori keşfini artırır.",
            treatmentOverrides = new Dictionary<string, double> { ["Diversity"] = 0.3 },
            treatmentShare = 0.5
        });
        var experiment = await created.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = experiment.GetProperty("id").GetGuid();
        Assert.Equal("Draft", experiment.GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/experiments/{id}/start", null)).StatusCode);
        try
        {
            var other = await (await admin.PostAsJsonAsync("/api/v1/admin/experiments", new
            {
                name = "İkinci", hypothesis = "Paralel deney olmamalı.",
                treatmentOverrides = new Dictionary<string, double> { ["Risk"] = 0.4 }, treatmentShare = 0.5
            })).Content.ReadFromJsonAsync<JsonElement>(Json);
            await AssertErrorAsync(await admin.PostAsync($"/api/v1/admin/experiments/{other.GetProperty("id").GetGuid()}/start", null),
                HttpStatusCode.Conflict, "EXPERIMENT_ALREADY_RUNNING");

            // Her iki gruptan bir kullanıcı bul ve günlük önerilerini üret.
            var seen = new HashSet<ExperimentVariant>();
            while (seen.Count < 2)
            {
                var (client, tokens, _) = await factory.RegisterAsync();
                var userId = tokens.GetProperty("userId").GetGuid();
                if (!seen.Add(ExperimentAssignment.VariantFor(userId, id, 0.5)))
                    continue;
                await CompleteOnboardingAsync(client);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/quests/today")).StatusCode);
            }

            var detail = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/experiments/{id}", Json);
            var results = detail.GetProperty("results");
            Assert.Equal(1, results.GetProperty("control").GetProperty("users").GetInt32());
            Assert.Equal(1, results.GetProperty("treatment").GetProperty("users").GetInt32());
            Assert.True(results.GetProperty("treatment").GetProperty("offered").GetInt32() > 0);
            Assert.Equal("InsufficientData", results.GetProperty("verdict").GetString());
        }
        finally
        {
            await admin.PostAsync($"/api/v1/admin/experiments/{id}/stop", null);
        }

        var adopted = await admin.PostAsJsonAsync($"/api/v1/admin/experiments/{id}/adopt", new { reason = "Test: kazananı uygula" });
        Assert.Equal("Adopted", (await adopted.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("outcome").GetString());

        var weights = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/recommendation-weights", Json);
        var diversity = weights.GetProperty("fields").EnumerateArray().Single(f => f.GetProperty("key").GetString() == "Diversity");
        Assert.Equal(0.3, diversity.GetProperty("value").GetDouble());

        var audit = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/audit?targetType=Experiment", Json);
        Assert.Contains(audit.GetProperty("items").EnumerateArray(), e => e.GetProperty("action").GetString() == "ExperimentAdopted");

        // Diğer testler için ağırlıkları varsayılana döndür.
        await admin.PostAsJsonAsync("/api/v1/admin/recommendation-weights/reset",
            new { revision = weights.GetProperty("revision").GetInt32(), keys = new[] { "Diversity" }, reason = "Test sonu" });
    }

    // ── Topluluk fikirleri ───────────────────────────────────────────────────

    [Fact]
    public async Task Ideas_are_screened_limited_and_reviewed_by_admins()
    {
        var user = await factory.CreateUserClientAsync();
        var admin = await factory.CreateAdminClientAsync();

        await AssertErrorAsync(await SubmitAsync(user, "Etkinlik sayfası", "Ayrıntılar için https://ornek.com adresine bakıp kaydol ve katıl."),
            HttpStatusCode.BadRequest, "Description");

        var first = await IdAsync(await SubmitAsync(user, "Mahalle kütüphanesi turu", "Mahallendeki kütüphaneye git ve bir rafı baştan sona incele."));
        var second = await IdAsync(await SubmitAsync(user, "Balkon bahçesi başlangıcı", "Bir saksıya fesleğen ek, her gün iki dakika ona bak ve not al."));
        var third = await IdAsync(await SubmitAsync(user, "Gece yarısı yürüyüşü", "Gece yarısı ıssız sokaklarda uzun bir yürüyüş yap ve şehri dinle."));
        await AssertErrorAsync(await SubmitAsync(user, "Dördüncü fikir", "Bekleyen fikir sınırı dolduğu için bu fikir kabul edilmemeli."),
            HttpStatusCode.Conflict, "IDEA_TOO_MANY_PENDING");

        var queue = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/ideas?status=Pending&pageSize=100", Json);
        var risky = queue.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == third);
        Assert.NotEmpty(risky.GetProperty("flags").EnumerateArray());
        Assert.False(risky.TryGetProperty("userId", out _));

        await AssertErrorAsync(await admin.PostAsJsonAsync($"/api/v1/admin/ideas/{third}/reject", new { note = "" }),
            HttpStatusCode.BadRequest, "Note");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/ideas/{third}/reject",
            new { note = "Gece ve ıssız yer güvenlik kurallarımıza uymuyor; gündüz bir versiyonunu önerebilirsin." })).StatusCode);

        var interests = await admin.GetFromJsonAsync<JsonElement>("/api/v1/catalog/interests", Json);
        var reading = interests.EnumerateArray().First(i => i.GetProperty("code").GetString() == "reading").GetProperty("id").GetGuid();
        var template = await admin.PostAsJsonAsync($"/api/v1/admin/templates?sourceIdeaId={first}", new
        {
            code = $"idea-library-{Guid.NewGuid():N}"[..28],
            title = "Mahalle kütüphanesi turu",
            description = "Mahallendeki kütüphaneye git ve bir rafı baştan sona incele.",
            type = "Weekly", difficulty = "Easy", category = "Learning", secondaryCategory = (string?)null,
            minMinutes = 30, maxMinutes = 60, cost = "Free", dayParts = new[] { "Morning", "Afternoon" },
            requiresCity = false, isOutdoor = false, cooldownDays = 14, riskScore = 0, interestIds = new[] { reading },
            effort = "Light", isStarter = false
        }, Json);
        Assert.Equal(HttpStatusCode.OK, template.StatusCode);

        var mine = (await user.GetFromJsonAsync<JsonElement>("/api/v1/ideas/mine", Json)).EnumerateArray()
            .ToDictionary(i => i.GetProperty("id").GetGuid());
        Assert.Equal("Accepted", mine[first].GetProperty("status").GetString());
        Assert.Equal("Rejected", mine[third].GetProperty("status").GetString());
        Assert.Contains("gündüz", mine[third].GetProperty("reviewNote").GetString());

        Assert.Equal(HttpStatusCode.OK, (await user.DeleteAsync($"/api/v1/ideas/{second}")).StatusCode);
        await AssertErrorAsync(await user.DeleteAsync($"/api/v1/ideas/{first}"), HttpStatusCode.Conflict, "IDEA_ALREADY_REVIEWED");
    }

    // ── KVKK dışa aktarma ────────────────────────────────────────────────────

    [Fact]
    public async Task Data_export_contains_only_the_users_own_data_and_no_secrets()
    {
        var (other, _, otherEmail) = await factory.RegisterAsync();
        await CompleteOnboardingAsync(other);
        var (client, _, email) = await factory.RegisterAsync();
        await CompleteOnboardingAsync(client);
        await client.GetAsync("/api/v1/quests/today");

        var response = await client.GetAsync("/api/v1/profile/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.Equal(email, json.GetProperty("account").GetProperty("email").GetString());
        Assert.Equal(3, json.GetProperty("quests").GetArrayLength());
        Assert.Equal("İstanbul", json.GetProperty("profile").GetProperty("city").GetString());
        Assert.DoesNotContain(otherEmail, body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/profile/export")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/profile/export")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/v1/profile/export")).StatusCode);
    }

    private static Task<HttpResponseMessage> SubmitAsync(HttpClient client, string title, string description)
        => client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title, description, category = "Learning", minutes = 45, cost = "Free", isOutdoor = false
        });

    private static async Task<Guid> IdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {body}");
        Assert.Contains(code, body);
    }
}
