using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LifeQuest.Domain.Catalog;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RealWorldTests(LifeQuestApiFactory factory)
{
    private async Task<(HttpClient Client, Guid UserId)> UserInCityAsync(string? city)
    {
        var (client, tokens, _) = await factory.RegisterAsync();
        await CompleteOnboardingAsync(client);
        var body = city is null ? (object)new { clearCity = true } : new { city };
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/api/v1/profile/preferences", body, Json)).StatusCode);
        return (client, tokens.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task Rain_keeps_outdoor_daily_quests_out_and_is_explained()
    {
        factory.Weather.Set("Yağmurköy", temperatureC: 14, precipitationProbability: 90, weatherCode: 63);
        var (client, userId) = await UserInCityAsync("Yağmurköy");

        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var weather = today.GetProperty("weather");
        Assert.Equal("Poor", weather.GetProperty("outdoor").GetString());
        Assert.Equal("Yağmurlu", weather.GetProperty("summary").GetString());
        Assert.Contains("açık hava", weather.GetProperty("advice").GetString());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>();
        var outdoorDaily = await db.UserQuests.Where(q => q.UserId == userId && q.Type == QuestType.Daily)
            .Join(db.QuestTemplates, q => q.TemplateId, t => t.Id, (q, t) => t.IsOutdoor)
            .CountAsync(isOutdoor => isOutdoor);
        Assert.Equal(0, outdoorDaily);
    }

    [Fact]
    public async Task Good_weather_and_unknown_city_are_reported_without_advice()
    {
        factory.Weather.Set("Güneşli", temperatureC: 22, precipitationProbability: 0, weatherCode: 0);
        var (sunny, _) = await UserInCityAsync("Guneşli");  // Farklı yazım aynı şehre eşlenir.
        var weather = (await sunny.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json)).GetProperty("weather");
        Assert.Equal("Good", weather.GetProperty("outdoor").GetString());
        Assert.Equal(22, weather.GetProperty("temperatureC").GetInt32());
        Assert.Equal(JsonValueKind.Null, weather.GetProperty("advice").ValueKind);

        var (noCity, _) = await UserInCityAsync(null);
        Assert.Equal(JsonValueKind.Null, (await noCity.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json)).GetProperty("weather").ValueKind);
    }

    [Fact]
    public async Task Admin_links_an_event_to_quests_and_users_in_that_city_see_it()
    {
        var admin = await factory.CreateAdminClientAsync();
        var (client, userId) = await UserInCityAsync("Etkinlikşehir");
        var (elsewhere, otherId) = await UserInCityAsync("Başkaşehir");

        var questIds = Ids(await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json));
        var otherQuestIds = Ids(await elsewhere.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json));
        await client.PostAsync($"/api/v1/quests/{questIds[0]}/accept", null);

        Guid[] templateIds;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>();
            templateIds = await db.UserQuests.Where(q => q.UserId == userId || q.UserId == otherId)
                .Select(q => q.TemplateId).Distinct().ToArrayAsync();
        }

        var start = DateTime.UtcNow.AddDays(2);
        var created = await admin.PostAsJsonAsync("/api/v1/admin/places", new
        {
            kind = "Event", city = "etkinlikşehir", name = "Açık hava film gecesi", address = "Sahil parkı",
            url = "https://example.com/film", startsAt = start, endsAt = start.AddHours(3), templateIds
        }, Json);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var place = await created.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(templateIds.Length, place.GetProperty("templates").GetArrayLength());

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{questIds[0]}", Json);
        var nearby = Assert.Single(detail.GetProperty("nearbyPlaces").EnumerateArray());
        Assert.Equal("Açık hava film gecesi", nearby.GetProperty("name").GetString());
        Assert.Equal("Event", nearby.GetProperty("kind").GetString());

        // Başka şehirdeki kullanıcı görmez.
        var other = await elsewhere.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{otherQuestIds[0]}", Json);
        Assert.Equal(0, other.GetProperty("nearbyPlaces").GetArrayLength());

        var listed = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/places?city=ETKİNLİKŞEHİR", Json);
        Assert.Single(listed.EnumerateArray());

        var id = place.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/v1/admin/places/{id}")).StatusCode);
        var audit = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/audit?targetType=LocalPlace", Json);
        Assert.Contains(audit.GetProperty("items").EnumerateArray(), e => e.GetProperty("action").GetString() == "PlaceDeleted");
    }

    [Fact]
    public async Task Places_are_validated_and_admin_only()
    {
        var admin = await factory.CreateAdminClientAsync();
        var user = await factory.CreateUserClientAsync();
        var anyTemplate = (await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/templates?pageSize=1", Json))
            .GetProperty("items")[0].GetProperty("id").GetGuid();

        var venue = new { kind = "Venue", city = "İzmir", name = "Kordon", templateIds = new[] { anyTemplate } };
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/v1/admin/places", venue, Json)).StatusCode);

        await AssertErrorAsync(await admin.PostAsJsonAsync("/api/v1/admin/places",
            new { kind = "Event", city = "İzmir", name = "Tarihsiz", templateIds = new[] { anyTemplate } }, Json),
            HttpStatusCode.BadRequest, "StartsAt");
        await AssertErrorAsync(await admin.PostAsJsonAsync("/api/v1/admin/places",
            new { kind = "Venue", city = "İzmir", name = "Bilinmeyen", templateIds = new[] { Guid.NewGuid() } }, Json),
            HttpStatusCode.BadRequest, "TemplateIds");
        await AssertErrorAsync(await admin.PostAsJsonAsync("/api/v1/admin/places",
            new { kind = "Venue", city = "İzmir", name = "Güvensiz", url = "http://example.com", templateIds = new[] { anyTemplate } }, Json),
            HttpStatusCode.BadRequest, "Url");

        var created = await admin.PostAsJsonAsync("/api/v1/admin/places", venue, Json);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/places/{id}", new
        {
            kind = "Venue", city = "İzmir", name = "Kordon boyu", templateIds = new[] { anyTemplate }, isActive = false
        }, Json);
        var body = await updated.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Kordon boyu", body.GetProperty("name").GetString());
        Assert.False(body.GetProperty("isActive").GetBoolean());
        await admin.DeleteAsync($"/api/v1/admin/places/{id}");
    }

    private static List<Guid> Ids(JsonElement list)
        => list.GetProperty("quests").EnumerateArray().Select(q => q.GetProperty("id").GetGuid()).ToList();

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {body}");
        Assert.Contains(code, body);
    }
}
