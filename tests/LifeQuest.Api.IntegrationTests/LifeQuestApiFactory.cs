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
    }

    public async Task<HttpClient> CreateUserClientAsync(string? email = null)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = email ?? $"user-{Guid.NewGuid():N}@example.com",
            password = "Passw0rd!",
            displayName = "Test Kullanıcı",
            birthYear = 1990
        });
        response.EnsureSuccessStatusCode();

        var tokens = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        return client;
    }

    public static async Task CompleteOnboardingAsync(HttpClient client, string radius = "Explore")
    {
        var response = await client.PutAsJsonAsync("/api/v1/profile/onboarding", new
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
