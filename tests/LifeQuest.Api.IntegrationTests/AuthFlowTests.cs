using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthFlowTests(LifeQuestApiFactory factory)
{
    [Fact]
    public async Task Refresh_token_rotation_detects_reuse_and_revokes_the_family()
    {
        var client = factory.CreateClient();
        var email = $"rot-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "Passw0rd!", displayName = "Rotasyon", birthYear = 1990 });
        var original = (await register.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("refreshToken").GetString();

        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var next = (await rotated.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("refreshToken").GetString();

        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var afterTheft = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = next });
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);
    }

    [Fact]
    public async Task Account_is_locked_after_repeated_failed_logins()
    {
        var email = $"lock-{Guid.NewGuid():N}@example.com";
        await factory.CreateUserClientAsync(email);
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPass1" });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("ACCOUNT_LOCKED", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Deleting_account_removes_all_user_data()
    {
        var email = $"del-{Guid.NewGuid():N}@example.com";
        var client = await factory.CreateUserClientAsync(email);
        await CompleteOnboardingAsync(client);
        await client.GetAsync("/api/v1/quests/today");

        var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/profile")
        {
            Content = JsonContent.Create(new { password = "Passw0rd!" })
        };
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(delete)).StatusCode);

        var login = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_token()
        => Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/v1/progress")).StatusCode);
}
