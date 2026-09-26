using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
}
