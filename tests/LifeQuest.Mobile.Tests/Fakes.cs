using System.Collections.Concurrent;
using System.Net;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Tests;

/// <summary>Yol ve yönteme göre sabit JSON yanıtlayan sahte API.</summary>
public sealed class FakeApi
{
    private readonly ConcurrentDictionary<string, Func<string, HttpResponseMessage>> _routes = new();

    public FakeApi()
    {
        Transport = new FakeTransport((request, body) =>
        {
            var key = $"{request.Method} {request.RequestUri!.PathAndQuery.Replace("/api/v1/", "")}";
            var route = _routes.FirstOrDefault(r => key == r.Key || (r.Key.EndsWith('*') && key.StartsWith(r.Key[..^1])));
            return Task.FromResult(route.Value is null
                ? FakeTransport.JsonResponse(HttpStatusCode.NotFound, "[]")
                : route.Value(body));
        });
        Store = new MemorySecureStore();
        (Api, Session, Auth) = ApiClientFactory.Create(new Uri("https://api.test"), Store, () => Transport);
    }

    public FakeTransport Transport { get; }
    public MemorySecureStore Store { get; }
    public LifeQuestApi Api { get; }
    public SessionStore Session { get; }
    public AuthService Auth { get; }

    public FakeApi On(string route, string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[route] = _ => FakeTransport.JsonResponse(status, json);
        return this;
    }

    public FakeApi On(string route, Func<string, HttpResponseMessage> respond)
    {
        _routes[route] = respond;
        return this;
    }

    public IEnumerable<(string Key, string Body)> Calls => Transport.Requests.Select(r =>
        ($"{r.Request.Method} {r.Request.RequestUri!.PathAndQuery.Replace("/api/v1/", "")}", r.Body));

    public async Task SignInAsync() => await Session.StartAsync(new AuthSession(Guid.Empty, "a", DateTime.UtcNow, DateTime.UtcNow, "r"));
}

public sealed class FakeNavigator : INavigator
{
    public List<string> Visited { get; } = [];
    public List<AppRoot> Roots { get; } = [];
    public IDictionary<string, object>? LastParameters { get; private set; }

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        Visited.Add(route);
        LastParameters = parameters;
        return Task.CompletedTask;
    }

    public Task BackAsync()
    {
        Visited.Add("..");
        return Task.CompletedTask;
    }

    public Task ShowRootAsync(AppRoot root)
    {
        Roots.Add(root);
        return Task.CompletedTask;
    }
}

public sealed class FakeToast : IToast
{
    public List<(string Kind, string Message)> Messages { get; } = [];
    public void Show(string message) => Messages.Add(("info", message));
    public void Success(string message) => Messages.Add(("success", message));
    public void Error(string message) => Messages.Add(("error", message));
}

public sealed class FakePreferences : IAppPreferences
{
    public Dictionary<string, string> Values { get; } = [];
    public string? Get(string key) => Values.GetValueOrDefault(key);

    public void Set(string key, string? value)
    {
        if (value is null) Values.Remove(key);
        else Values[key] = value;
    }
}

public sealed class FakeNotifications : ILocalNotifications
{
    public bool Allow { get; set; } = true;
    public List<(int Id, DateTime At, string Title)> Scheduled { get; } = [];
    public Task<bool> RequestPermissionAsync() => Task.FromResult(Allow);

    public Task CancelAllAsync()
    {
        Scheduled.Clear();
        return Task.CompletedTask;
    }

    public Task ScheduleAsync(int id, DateTime localTime, string title, string body)
    {
        Scheduled.Add((id, localTime, title));
        return Task.CompletedTask;
    }
}

public sealed class FakeShare : IShare
{
    public List<string> Shared { get; } = [];
    public Task ShareTextAsync(string title, string text) { Shared.Add(text); return Task.CompletedTask; }
    public Task ShareFileAsync(string title, DownloadedFile file) { Shared.Add(file.FileName); return Task.CompletedTask; }
    public Task OpenBrowserAsync(string url) { Shared.Add(url); return Task.CompletedTask; }
}

public sealed class FakeAppInfo : IAppInfo
{
    public string WebBaseUrl => "https://lifequest.test";
    public string TimeZoneId => "Europe/Istanbul";
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public static class Json
{
    public const string Profile = """{"userId":"00000000-0000-0000-0000-000000000000","displayName":"Alex Doe","email":"a@b.c","onboardingCompleted":true,"discoveryRadius":"Explore","budget":"Low","weeklyAvailableMinutes":300,"goals":["Culture"],"city":"Istanbul","timeZoneId":"Europe/Istanbul","maxPhysicalEffort":"Moderate","notificationPreference":"WeeklySummary","dailyReminderHour":null,"interests":[{"id":"00000000-0000-0000-0000-000000000001","code":"coffee","name":"Coffee","category":"Explorer","weight":0.9,"source":"Explicit"}],"language":"en"}""";

    public static string Quest(string id, string status, string title = "Walk") =>
        $$"""{"id":"{{id}}","title":"{{title}}","description":"d","type":"Daily","difficulty":"Easy","category":"Fitness","secondaryCategory":null,"minMinutes":20,"maxMinutes":30,"cost":"Free","effort":"Light","reward":{"lifeXp":38,"primaryCategoryXp":30,"secondaryCategoryXp":0},"status":"{{status}}","source":"Daily","offeredAt":"2026-09-26T06:00:00Z","expiresAt":"2026-09-27T01:00:00Z","acceptedAt":null,"completedAt":null,"skipReason":null,"rating":null,"preference":null,"isExploration":false,"explanation":"Suggested because.","plannedAt":null}""";

    public static string Detail(string quest) =>
        $$"""{"quest":{{quest}},"score":{"interest":0.8,"novelty":0.5,"context":1.2,"goalFit":0.3,"diversity":0.1,"feedbackFit":0,"repetition":0.2,"friction":0,"risk":0,"total":0.9},"reasonCodes":[],"nearbyPlaces":[],"party":null}""";
}
