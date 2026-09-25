using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LifeQuest.Infrastructure.Persistence;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AdminManagementTests(LifeQuestApiFactory factory)
{
    // ── Yetki ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("GET", "/api/v1/admin/users")]
    [InlineData("POST", "/api/v1/admin/users/00000000-0000-0000-0000-000000000001/suspend")]
    [InlineData("GET", "/api/v1/admin/templates")]
    [InlineData("GET", "/api/v1/admin/catalog/health")]
    [InlineData("GET", "/api/v1/admin/recommendation-weights")]
    [InlineData("GET", "/api/v1/admin/audit")]
    public async Task Regular_users_cannot_reach_admin_endpoints(string method, string url)
    {
        var client = await factory.CreateUserClientAsync();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { reason = "x" })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Kullanıcılar ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_finds_users_and_sees_only_account_fields_and_totals()
    {
        var admin = await factory.CreateAdminClientAsync();
        var (_, _, email) = await factory.RegisterAsync();

        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/users?search={email[..12]}", Json);
        var user = Assert.Single(page.GetProperty("items").EnumerateArray());

        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal(0, user.GetProperty("completedQuests").GetInt32());
        Assert.False(user.GetProperty("isSuspended").GetBoolean());
        var fields = user.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("interests", fields);
        Assert.DoesNotContain("quests", fields);
        Assert.DoesNotContain("passwordHash", fields);
    }

    [Fact]
    public async Task Suspension_takes_effect_immediately_and_can_be_lifted()
    {
        var admin = await factory.CreateAdminClientAsync();
        var (user, tokens, email) = await factory.RegisterAsync();
        var id = await UserIdAsync(admin, email);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/v1/profile")).StatusCode);

        var suspend = await admin.PostAsJsonAsync($"/api/v1/admin/users/{id}/suspend", new { days = 7, reason = "Spam içerik" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.True((await suspend.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("isSuspended").GetBoolean());

        // Mevcut access token süresi dolmadan reddedilir; refresh ve giriş de kapalıdır.
        await AssertErrorAsync(await user.GetAsync("/api/v1/profile"), HttpStatusCode.Unauthorized, "ACCOUNT_SUSPENDED");
        var anonymous = factory.CreateClient();
        await AssertErrorAsync(await anonymous.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = tokens.GetProperty("refreshToken").GetString() }), HttpStatusCode.Unauthorized, "INVALID_REFRESH_TOKEN");
        await AssertErrorAsync(await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password }),
            HttpStatusCode.Unauthorized, "ACCOUNT_SUSPENDED");

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/users/{id}/unsuspend", null)).StatusCode);
        var again = await factory.LoginAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await again.GetAsync("/api/v1/profile")).StatusCode);
    }

    [Fact]
    public async Task Role_change_makes_the_old_token_stale_and_refresh_picks_up_the_new_role()
    {
        var admin = await factory.CreateAdminClientAsync();
        var (user, tokens, email) = await factory.RegisterAsync();
        var id = await UserIdAsync(admin, email);

        var grant = await admin.PutAsJsonAsync($"/api/v1/admin/users/{id}/role", new { role = "admin" });
        Assert.Equal("admin", (await grant.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("role").GetString());

        await AssertErrorAsync(await user.GetAsync("/api/v1/profile"), HttpStatusCode.Unauthorized, "TOKEN_STALE");
        var refreshed = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = tokens.GetProperty("refreshToken").GetString() });
        Authorize(user, await refreshed.Content.ReadFromJsonAsync<JsonElement>(Json));
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/v1/admin/metrics")).StatusCode);

        // Yeni admin, config'ten gelen (bootstrap) admin'in rolünü alamaz; kendi rolünü de değiştiremez.
        var bootstrapId = await UserIdAsync(admin, AdminEmail);
        await AssertErrorAsync(await user.PutAsJsonAsync($"/api/v1/admin/users/{bootstrapId}/role", new { role = "user" }),
            HttpStatusCode.Conflict, "ADMIN_BOOTSTRAP");
        await AssertErrorAsync(await user.PutAsJsonAsync($"/api/v1/admin/users/{id}/role", new { role = "user" }),
            HttpStatusCode.Conflict, "ADMIN_SELF_ACTION");

        // Admin rolündeki hesap önce rolü alınmadan askıya alınamaz.
        await AssertErrorAsync(await admin.PostAsJsonAsync($"/api/v1/admin/users/{id}/suspend", new { reason = "Deneme" }),
            HttpStatusCode.Conflict, "ADMIN_TARGET_IS_ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{id}/role", new { role = "user" })).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_user_requires_email_confirmation_and_leaves_a_masked_audit_entry()
    {
        var admin = await factory.CreateAdminClientAsync();
        var (user, _, email) = await factory.RegisterAsync();
        var id = await UserIdAsync(admin, email);

        await AssertErrorAsync(await DeleteUserAsync(admin, id, "Hesap kapatma talebi", "yanlis@example.com"),
            HttpStatusCode.BadRequest, "ConfirmEmail");
        Assert.Equal(HttpStatusCode.OK, (await DeleteUserAsync(admin, id, "Hesap kapatma talebi", email.ToUpperInvariant())).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/users/{id}")).StatusCode);
        await AssertErrorAsync(await user.GetAsync("/api/v1/profile"), HttpStatusCode.Unauthorized, "ACCOUNT_SUSPENDED");

        var audit = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/audit?action=UserDeleted", Json);
        var entry = audit.GetProperty("items").EnumerateArray().First(e => e.GetProperty("targetId").GetGuid() == id);
        Assert.Equal($"{email[0]}***@example.com", entry.GetProperty("targetLabel").GetString());
        Assert.Equal("Hesap kapatma talebi", entry.GetProperty("reason").GetString());
        Assert.Equal(AdminEmail, entry.GetProperty("actorEmail").GetString());
    }

    // ── Katalog ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Template_with_rule_violations_waits_for_review_until_an_admin_approves_it()
    {
        var admin = await factory.CreateAdminClientAsync();
        var before = await HealthAsync(admin);
        var code = $"admin-risky-{Guid.NewGuid():N}"[..30];

        var validation = await admin.PostAsJsonAsync("/api/v1/admin/templates/validate", await TemplateAsync(admin, code, risk: 0.5), Json);
        var check = await validation.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("NeedsReview", check.GetProperty("resultingSafety").GetString());

        var created = await (await admin.PostAsJsonAsync("/api/v1/admin/templates", await TemplateAsync(admin, code, risk: 0.5), Json))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("NeedsReview", created.GetProperty("safety").GetString());
        Assert.Equal("Admin", created.GetProperty("source").GetString());
        Assert.NotEmpty(created.GetProperty("violations").EnumerateArray());
        Assert.Equal(before.GetProperty("offerable").GetInt32(), (await HealthAsync(admin)).GetProperty("offerable").GetInt32());

        await AssertErrorAsync(await admin.PostAsJsonAsync($"/api/v1/admin/templates/{id}/safety", new { safety = "Safe" }, Json),
            HttpStatusCode.BadRequest, "Note");
        var approved = await admin.PostAsJsonAsync($"/api/v1/admin/templates/{id}/safety",
            new { safety = "Safe", note = "Rehberli etkinlik, risk kabul edilebilir" }, Json);
        Assert.Equal("Safe", (await approved.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("safety").GetString());
        Assert.Equal(before.GetProperty("offerable").GetInt32() + 1, (await HealthAsync(admin)).GetProperty("offerable").GetInt32());

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/templates/{id}/deactivate", null)).StatusCode);
        Assert.Equal(before.GetProperty("offerable").GetInt32(), (await HealthAsync(admin)).GetProperty("offerable").GetInt32());
    }

    [Fact]
    public async Task New_starter_template_is_visible_immediately_because_the_catalog_cache_is_invalidated()
    {
        var admin = await factory.CreateAdminClientAsync();
        var user = await factory.CreateUserClientAsync();
        var code = $"admin-starter-{Guid.NewGuid():N}"[..30];
        await user.GetFromJsonAsync<JsonElement>("/api/v1/onboarding/starter-cards", Json); // cache'i ısıt

        var response = await admin.PostAsJsonAsync("/api/v1/admin/templates", await TemplateAsync(admin, code, starter: true), Json);
        Assert.Equal("Safe", (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("safety").GetString());

        var cards = await user.GetFromJsonAsync<JsonElement>("/api/v1/onboarding/starter-cards", Json);
        Assert.Contains(cards.EnumerateArray(), c => c.GetProperty("code").GetString() == code);

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("/api/v1/admin/templates", await TemplateAsync(admin, code), Json)).StatusCode);
    }

    [Fact]
    public async Task Admin_edit_of_a_seeded_template_survives_reseeding_and_stale_edits_conflict()
    {
        var admin = await factory.CreateAdminClientAsync();
        var list = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/templates?text=culture-classic-film", Json);
        var id = list.GetProperty("items")[0].GetProperty("id").GetGuid();
        var template = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/templates/{id}", Json);
        var version = template.GetProperty("version").GetInt32();

        var input = ToInput(template, title: "Klasik bir filmi yeniden keşfet (admin)");
        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/templates/{id}", new { version, template = input }, Json);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(version + 1, (await updated.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("version").GetInt32());

        await AssertErrorAsync(await admin.PutAsJsonAsync($"/api/v1/admin/templates/{id}", new { version, template = input }, Json),
            HttpStatusCode.Conflict, "ADMIN_STALE_VERSION");

        await factory.Services.InitializeLifeQuestDatabaseAsync();

        var after = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/templates/{id}", Json);
        Assert.Equal("Klasik bir filmi yeniden keşfet (admin)", after.GetProperty("title").GetString());
        Assert.Equal("Admin", after.GetProperty("source").GetString());
    }

    // ── Öneri ağırlıkları ────────────────────────────────────────────────────

    [Fact]
    public async Task Weights_can_be_changed_within_limits_and_reset_with_an_audit_trail()
    {
        var admin = await factory.CreateAdminClientAsync();
        var initial = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/recommendation-weights", Json);
        var revision = initial.GetProperty("revision").GetInt32();
        var diversityDefault = Field(initial, "Diversity").GetProperty("defaultValue").GetDouble();

        await AssertErrorAsync(await admin.PutAsJsonAsync("/api/v1/admin/recommendation-weights",
            new { revision, values = new Dictionary<string, double> { ["Risk"] = 2 }, reason = "Deneme" }),
            HttpStatusCode.BadRequest, "Values.Risk");

        var updated = await (await admin.PutAsJsonAsync("/api/v1/admin/recommendation-weights",
            new { revision, values = new Dictionary<string, double> { ["Diversity"] = 0.2 }, reason = "Çeşitliliği artır" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(revision + 1, updated.GetProperty("revision").GetInt32());
        Assert.Equal(0.2, Field(updated, "Diversity").GetProperty("value").GetDouble());
        Assert.True(Field(updated, "Diversity").GetProperty("isOverridden").GetBoolean());
        Assert.Equal(AdminEmail, updated.GetProperty("updatedBy").GetString());

        await AssertErrorAsync(await admin.PutAsJsonAsync("/api/v1/admin/recommendation-weights",
            new { revision, values = new Dictionary<string, double> { ["Risk"] = 0.2 }, reason = "Eski sürüm" }),
            HttpStatusCode.Conflict, "ADMIN_STALE_VERSION");

        // Öneri üretimi yeni ağırlıklarla çalışır (uç çağrısı hata vermez).
        var user = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(user);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/v1/quests/today")).StatusCode);

        var reset = await (await admin.PostAsJsonAsync("/api/v1/admin/recommendation-weights/reset",
            new { revision = revision + 1, keys = (string[]?)null, reason = "Test sonu" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(diversityDefault, Field(reset, "Diversity").GetProperty("value").GetDouble());
        Assert.False(Field(reset, "Diversity").GetProperty("isOverridden").GetBoolean());

        var audit = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/audit?targetType=RecommendationSettings", Json);
        var actions = audit.GetProperty("items").EnumerateArray().Take(2).Select(e => e.GetProperty("action").GetString()).ToList();
        Assert.Equal(["WeightsReset", "WeightsUpdated"], actions);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static async Task<Guid> UserIdAsync(HttpClient admin, string email)
    {
        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/users?search={Uri.EscapeDataString(email)}", Json);
        return page.GetProperty("items").EnumerateArray().Single(u => u.GetProperty("email").GetString() == email).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> DeleteUserAsync(HttpClient admin, Guid id, string reason, string confirmEmail)
        => admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/users/{id}")
        {
            Content = JsonContent.Create(new { reason, confirmEmail })
        });

    private static async Task<JsonElement> HealthAsync(HttpClient admin)
        => await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/catalog/health", Json);

    private static JsonElement Field(JsonElement weights, string key)
        => weights.GetProperty("fields").EnumerateArray().Single(f => f.GetProperty("key").GetString() == key);

    private static async Task<object> TemplateAsync(HttpClient client, string code, double risk = 0, bool starter = false)
    {
        var interests = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/interests", Json);
        var reading = interests.EnumerateArray().First(i => i.GetProperty("code").GetString() == "reading").GetProperty("id").GetGuid();
        return new
        {
            code,
            title = "Sahafta bir saat geçir",
            description = "Yakınındaki bir sahafı ziyaret et ve rastgele seçtiğin bir kitabın ilk sayfasını oku.",
            type = "Weekly",
            difficulty = "Easy",
            category = "Learning",
            secondaryCategory = (string?)null,
            minMinutes = 45,
            maxMinutes = 90,
            cost = "Free",
            dayParts = new[] { "Morning", "Afternoon" },
            requiresCity = false,
            isOutdoor = false,
            cooldownDays = 14,
            riskScore = risk,
            interestIds = new[] { reading },
            effort = "Light",
            isStarter = starter
        };
    }

    private static Dictionary<string, object?> ToInput(JsonElement template, string title)
    {
        string[] keys = ["code", "title", "description", "type", "difficulty", "category", "secondaryCategory", "minMinutes",
            "maxMinutes", "cost", "dayParts", "requiresCity", "isOutdoor", "cooldownDays", "riskScore", "interestIds", "effort", "isStarter"];
        var input = keys.ToDictionary(k => k, k => (object?)template.GetProperty(k).Clone());
        input["title"] = title;
        return input;
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {body}");
        Assert.Contains(code, body);
    }
}
