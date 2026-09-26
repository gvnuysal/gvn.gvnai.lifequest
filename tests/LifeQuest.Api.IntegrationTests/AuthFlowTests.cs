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
        // Çerezsiz istemciler: token'lar gövdeyle (geçiş yolu) açıkça gönderilir.
        var client = factory.CreateClient(new() { HandleCookies = false, BaseAddress = new Uri("https://localhost") });
        var email = $"rot-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "Passw0rd!", displayName = "Rotasyon", birthYear = 1990 });
        var original = RefreshCookieOf(register);
        Assert.NotNull(original);

        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var next = RefreshCookieOf(rotated);
        Assert.NotEqual(original, next);

        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var afterTheft = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = next });
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_lives_only_in_an_http_only_cookie()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"cookie-{Guid.NewGuid():N}@example.com", password = "Passw0rd!", displayName = "Çerez", birthYear = 1990 });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var body = await register.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.False(body.TryGetProperty("refreshToken", out _));
        Assert.True(body.TryGetProperty("refreshTokenExpiresAt", out _));

        var setCookie = Assert.Single(register.Headers.GetValues("Set-Cookie"), c => c.StartsWith(RefreshCookieName + "="));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase);

        // Gövdesiz yenileme çerezle çalışır; çerez de döner (rotasyon).
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotNull(RefreshCookieOf(refreshed));

        // Çıkış çerezi siler ve oturum ailesini kapatır; sonraki yenileme reddedilir.
        Authorize(client, await refreshed.Content.ReadFromJsonAsync<JsonElement>(Json));
        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), c => c.StartsWith(RefreshCookieName + "=;"));
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
    }

    [Fact]
    public async Task Native_client_gets_the_refresh_token_in_the_body_and_never_a_cookie()
    {
        var client = NativeClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"native-{Guid.NewGuid():N}@example.com", password = Password, displayName = "Mobil", birthYear = 1990 });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        Assert.Null(RefreshCookieOf(register));
        var original = (await register.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(original));

        // Gövdeyle yenileme: yeni token yine gövdede, rotasyonlu.
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Null(RefreshCookieOf(refreshed));
        var body = await refreshed.Content.ReadFromJsonAsync<JsonElement>(Json);
        var next = body.GetProperty("refreshToken").GetString();
        Assert.NotEqual(original, next);

        // Eski token'ın yeniden kullanımı aileyi kapatır.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = next })).StatusCode);

        // Giriş de aynı: çerez yok, token gövdede.
        var email = $"native-login-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password, displayName = "Mobil", birthYear = 1990 });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Null(RefreshCookieOf(login));
        Assert.True((await login.Content.ReadFromJsonAsync<JsonElement>(Json)).TryGetProperty("refreshToken", out _));
    }

    [Fact]
    public async Task Native_header_cannot_lift_the_web_cookie_into_the_body()
    {
        // Web oturumu (çerezli) açılır; aynı istemci başlığı ekleyip gövdesiz yenilemeyi dener.
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"lift-{Guid.NewGuid():N}@example.com", password = Password, displayName = "Web", birthYear = 1990 });
        Assert.NotNull(RefreshCookieOf(register));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh") { Content = JsonContent.Create(new { }) };
        request.Headers.Add("X-LifeQuest-Client", "native");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Çerezli web yenilemesi etkilenmez ve gövdede token dönmez.
        var web = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.OK, web.StatusCode);
        Assert.False((await web.Content.ReadFromJsonAsync<JsonElement>(Json)).TryGetProperty("refreshToken", out _));
    }

    private HttpClient NativeClient()
    {
        var client = factory.CreateClient(new() { HandleCookies = false, BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-LifeQuest-Client", "native");
        return client;
    }

    [Fact]
    public async Task Refresh_without_cookie_or_body_is_unauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("INVALID_REFRESH_TOKEN", await response.Content.ReadAsStringAsync());
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
