using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    }

    public const string Password = "Passw0rd!";

    private readonly SemaphoreSlim _adminLock = new(1, 1);
    private bool _adminRegistered;

    public async Task<HttpClient> CreateUserClientAsync(string? email = null) => (await RegisterAsync(email)).Client;

    /// <summary>Kaydolur; istemci access token'la yetkilendirilir, ham token'lar (refresh dahil) da döner.</summary>
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

        var tokens = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
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

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<LifeQuestApiFactory>
{
    public const string Name = "api";
}
