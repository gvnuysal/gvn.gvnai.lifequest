using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.RealWorld;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace LifeQuest.Api.IntegrationTests;

/// <summary>Gerçek PostgreSQL (Testcontainers) üzerinde tüm API'yi ayağa kaldırır.</summary>
public sealed class LifeQuestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public const string AdminEmail = "admin@lifequest.test";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public LifeQuestApiFactory()
    {
        // Refresh token çerezi Secure: istemci HTTPS adresle çalışmazsa CookieContainer çerezi geri göndermez.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public const string RefreshCookieName = "lq_refresh";

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LifeQuest", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-test-secret-key-at-least-32-bytes!");
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", "1000");
        builder.UseSetting("Admin:BootstrapEmails:0", AdminEmail);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IPushSender>(Push);
            services.AddSingleton<IWeatherProvider>(Weather);
        });
    }

    /// <summary>Dış servis yerine şehir adına göre sabit hava döner; tanımsız şehir için null.</summary>
    public FakeWeatherProvider Weather { get; } = new();

    /// <summary>Gerçek push servisi yerine gönderilenleri kaydeder.</summary>
    public FakePushSender Push { get; } = new();

    public const string Password = "Passw0rd!";

    private readonly SemaphoreSlim _adminLock = new(1, 1);
    private bool _adminRegistered;

    public async Task<HttpClient> CreateUserClientAsync(string? email = null) => (await RegisterAsync(email)).Client;

    /// <summary>
    /// Kaydolur; istemci access token'la yetkilendirilir ve refresh çerezini taşır. Test kolaylığı için çerezdeki
    /// refresh token da dönen Tokens'a "refreshToken" olarak eklenir (API gövdede döndürmez).
    /// </summary>
    public async Task<(HttpClient Client, JsonElement Tokens, string Email)> RegisterAsync(string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = Password,
            displayName = "Test Kullanıcı",
            birthYear = 1990
        });
        response.EnsureSuccessStatusCode();

        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(Json))!;
        body["refreshToken"] = RefreshCookieOf(response);
        var tokens = JsonSerializer.SerializeToElement(body, Json);
        Authorize(client, tokens);
        return (client, tokens, email);
    }

    public async Task<HttpClient> LoginAsync(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        Authorize(client, await response.Content.ReadFromJsonAsync<JsonElement>(Json));
        return client;
    }

    /// <summary>Bootstrap admin hesabı fixture başına bir kez kaydedilir; sonraki çağrılar giriş yapar.</summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        await _adminLock.WaitAsync();
        try
        {
            if (_adminRegistered)
                return await LoginAsync(AdminEmail);

            _adminRegistered = true;
            return await CreateUserClientAsync(AdminEmail);
        }
        finally
        {
            _adminLock.Release();
        }
    }

    /// <summary>Yanıttaki Set-Cookie başlığından refresh token değerini okur.</summary>
    public static string? RefreshCookieOf(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;
        var cookie = cookies.FirstOrDefault(c => c.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        var value = cookie?.Split(';')[0][(RefreshCookieName.Length + 1)..];
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static void Authorize(HttpClient client, JsonElement tokens)
        => client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());

    public static async Task CompleteOnboardingAsync(HttpClient client, string radius = "Explore", object? extra = null)
    {
        var response = await client.PutAsJsonAsync("/api/v1/profile/onboarding", extra ?? new
        {
            goals = new[] { "Culture", "Creativity" },
            interests = new[] { new { code = "coffee", weight = 0.9 }, new { code = "cinema", weight = 0.8 }, new { code = "art", weight = 0.4 } },
            weeklyAvailableMinutes = 300,
            budget = "Medium",
            discoveryRadius = radius,
            city = "İstanbul",
            timeZoneId = "Europe/Istanbul"
        }, Json);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class FakePushSender : IPushSender
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<(string Endpoint, PushNotification Notification)> _sent = new();

    public bool IsConfigured => true;
    public string PublicKey => "BFakePublicKeyForTests";

    public IReadOnlyList<(string Endpoint, PushNotification Notification)> Sent => _sent.ToList();

    public Task<PushDelivery> SendAsync(PushTarget target, PushNotification notification, CancellationToken cancellationToken)
    {
        // Adresinde "gone" geçen abonelik push servisinde silinmiş gibi davranır.
        if (target.Endpoint.Contains("gone", StringComparison.Ordinal))
            return Task.FromResult(PushDelivery.Expired);
        _sent.Enqueue((target.Endpoint, notification));
        return Task.FromResult(PushDelivery.Sent);
    }
}

public sealed class FakeWeatherProvider : IWeatherProvider
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, WeatherSnapshot> _byCity = new();

    /// <summary>Şehir için önümüzdeki 48 saat boyunca aynı koşullar.</summary>
    public void Set(string city, double temperatureC, int precipitationProbability, int weatherCode)
    {
        var start = DateTime.UtcNow.Date.AddDays(-1);
        var hours = Enumerable.Range(0, 96)
            .Select(h => new HourlyWeather(start.AddHours(h), temperatureC, precipitationProbability, weatherCode, 10))
            .ToList();
        _byCity[CityKey.Normalize(city)] = new WeatherSnapshot(city, DateTime.UtcNow, temperatureC, weatherCode, 10, true, hours);
    }

    public Task<WeatherSnapshot?> GetAsync(string city, CancellationToken cancellationToken)
        => Task.FromResult(_byCity.TryGetValue(CityKey.Normalize(city), out var snapshot) ? snapshot : null);
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<LifeQuestApiFactory>
{
    public const string Name = "api";
}
