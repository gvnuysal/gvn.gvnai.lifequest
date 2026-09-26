using System.Net;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using static LifeQuest.Mobile.Tests.FakeTransport;

namespace LifeQuest.Mobile.Tests;

public sealed class AuthHandlerTests
{
    private static string Session(string access, string refresh) =>
        $$"""{"userId":"{{Guid.Empty}}","accessToken":"{{access}}","accessTokenExpiresAt":"2030-01-01T00:00:00Z","refreshTokenExpiresAt":"2030-01-01T00:00:00Z","refreshToken":"{{refresh}}"}""";

    private static (LifeQuestApi Api, SessionStore Session, FakeTransport Transport, MemorySecureStore Store) Create(
        Func<HttpRequestMessage, string, Task<HttpResponseMessage>> respond)
    {
        var transport = new FakeTransport(respond);
        var store = new MemorySecureStore();
        var (api, session, _) = ApiClientFactory.Create(new Uri("https://api.test"), store, () => transport);
        return (api, session, transport, store);
    }

    [Fact]
    public async Task Parallel_401s_share_one_refresh_and_requests_are_retried()
    {
        var refreshes = 0;
        var (api, session, transport, store) = Create(async (request, body) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("auth/refresh"))
            {
                Interlocked.Increment(ref refreshes);
                await Task.Delay(50);
                Assert.Contains("\"refreshToken\":\"r1\"", body);
                return JsonResponse(HttpStatusCode.OK, Session("a2", "r2"));
            }

            return request.Headers.Authorization?.Parameter == "a2"
                ? JsonResponse(HttpStatusCode.OK, "[]")
                : JsonResponse(HttpStatusCode.Unauthorized, """[{"code":"TOKEN_STALE","message":"x","type":"Unauthorized"}]""");
        });
        await session.StartAsync(new AuthSession(Guid.Empty, "a1", DateTime.UtcNow, DateTime.UtcNow, "r1"));

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => api.ActiveAsync()));

        Assert.All(results, Assert.Empty);
        Assert.Equal(1, refreshes);
        Assert.Equal("r2", store.Values[SessionStore.RefreshTokenKey]);
        Assert.All(transport.Requests, r =>
        {
            Assert.Equal("native", r.Request.Headers.GetValues("X-LifeQuest-Client").Single());
            Assert.NotEmpty(r.Request.Headers.AcceptLanguage);
        });
    }

    [Fact]
    public async Task Suspended_account_ends_the_session_without_refreshing()
    {
        var (api, session, transport, store) = Create((request, _) => Task.FromResult(
            JsonResponse(HttpStatusCode.Unauthorized, """[{"code":"ACCOUNT_SUSPENDED","message":"Askıda","type":"Unauthorized"}]""")));
        await session.StartAsync(new AuthSession(Guid.Empty, "a1", DateTime.UtcNow, DateTime.UtcNow, "r1"));
        SessionEndReason? ended = null;
        session.Ended += (_, reason) => ended = reason;

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.ActiveAsync());

        Assert.True(ex.Has("ACCOUNT_SUSPENDED"));
        Assert.Equal(SessionEndReason.Suspended, ended);
        Assert.False(session.HasSession);
        Assert.Empty(store.Values);
        Assert.DoesNotContain(transport.Requests, r => r.Request.RequestUri!.AbsolutePath.EndsWith("auth/refresh"));
    }

    [Fact]
    public async Task Rejected_refresh_expires_the_session()
    {
        var (api, session, _, _) = Create((request, _) => Task.FromResult(
            JsonResponse(HttpStatusCode.Unauthorized, """[{"code":"INVALID_REFRESH_TOKEN","message":"x","type":"Unauthorized"}]""")));
        await session.StartAsync(new AuthSession(Guid.Empty, "a1", DateTime.UtcNow, DateTime.UtcNow, "r1"));
        SessionEndReason? ended = null;
        session.Ended += (_, reason) => ended = reason;

        await Assert.ThrowsAsync<ApiException>(() => api.ActiveAsync());

        Assert.Equal(SessionEndReason.Expired, ended);
        Assert.False(session.HasSession);
    }

    [Fact]
    public async Task Stored_refresh_token_restores_the_session_on_start()
    {
        var (_, session, _, store) = Create((request, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, Session("a9", "r9"))));
        await store.SetAsync(SessionStore.RefreshTokenKey, "r8");

        Assert.True(await session.RestoreAsync());
        Assert.Equal("a9", session.AccessToken);
        Assert.Equal("r9", store.Values[SessionStore.RefreshTokenKey]);
    }

    [Fact]
    public async Task Network_failure_keeps_the_stored_session()
    {
        var (_, session, _, store) = Create((_, _) => throw new HttpRequestException("offline"));
        await store.SetAsync(SessionStore.RefreshTokenKey, "r1");

        await Assert.ThrowsAsync<ApiException>(() => session.RestoreAsync());
        Assert.Equal("r1", store.Values[SessionStore.RefreshTokenKey]);
    }

    [Fact]
    public async Task Requests_are_sent_with_the_api_contract()
    {
        var (api, session, transport, _) = Create((request, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK,
            """{"userId":"00000000-0000-0000-0000-000000000000","displayName":"A","email":"a@b.c","onboardingCompleted":true,"discoveryRadius":"Explore","budget":"Low","weeklyAvailableMinutes":300,"goals":["Culture"],"city":null,"timeZoneId":"Europe/Istanbul","maxPhysicalEffort":"Moderate","notificationPreference":"WeeklySummary","dailyReminderHour":null,"interests":[],"language":"en"}""")));
        await session.StartAsync(new AuthSession(Guid.Empty, "a1", DateTime.UtcNow, DateTime.UtcNow, "r1"));

        var profile = await api.UpdatePreferencesAsync(new PreferencesRequest { Budget = CostBand.Low, Language = "en" });

        var (request, body) = transport.Requests.Last();
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://api.test/api/v1/profile/preferences", request.RequestUri!.ToString());
        Assert.Equal("""{"budget":"Low","language":"en"}""", body);
        Assert.Equal(DiscoveryRadius.Explore, profile.DiscoveryRadius);
        Assert.Equal([LifeCategory.Culture], profile.Goals);
    }
}
