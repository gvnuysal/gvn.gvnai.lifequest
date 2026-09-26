using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using System.Text.Json.Serialization;
using Gvn.GvnFramework.AspNetCore.Extensions;
using Gvn.GvnFramework.BackgroundJobs.Configuration;
using Gvn.GvnFramework.BackgroundJobs.DependencyInjection;
using Gvn.GvnFramework.Caching.Configuration;
using Gvn.GvnFramework.Caching.DependencyInjection;
using Gvn.GvnFramework.Logging.Configuration;
using Gvn.GvnFramework.Modularity;
using Gvn.GvnFramework.Security.Configuration;
using Gvn.GvnFramework.Security.DependencyInjection;
using Gvn.GvnFramework.Swagger.DependencyInjection;
using LifeQuest.Api.Infrastructure;
using LifeQuest.Application;
using LifeQuest.Application.Abstractions;
using LifeQuest.Infrastructure;
using LifeQuest.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ── Logging: framework Serilog yapılandırması ────────────────────────────────
// Maskeleme framework'te: şifre/token gibi bilinen adlar ve [Sensitive] işaretli alanlar (e-posta kısmi) loglanmaz.
builder.Services.AddSerilog(
    SerilogConfiguration.CreateDefaultConfiguration(configuration, "LifeQuest.Api").CreateLogger(),
    dispose: true);

// ── Web ──────────────────────────────────────────────────────────────────────
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(o => o.AddDefaultPolicy(policy => policy
    .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Refresh token HttpOnly çerezde: UI ayrı alan adındayken /auth çağrıları çerezi taşıyabilsin.
    .AllowCredentials()
    // UI ayrı alan adındayken indirilen dosyanın (.ics, veri dışa aktarma) adını okuyabilmesi için.
    .WithExposedHeaders("Content-Disposition")));

builder.Services.AddLifeQuestRateLimiting(configuration);
builder.Services.Configure<RefreshCookieOptions>(configuration.GetSection(RefreshCookieOptions.SectionName));
builder.Services.AddSingleton<RefreshTokenCookie>();

// ── Gvn.GvnFramework modülleri ───────────────────────────────────────────────
builder.Services.AddGvnSecurity(jwt =>
{
    configuration.GetSection(JwtOptions.SectionName).Bind(jwt);
    if (string.IsNullOrWhiteSpace(jwt.Secret) || Encoding.UTF8.GetByteCount(jwt.Secret) < 32)
        throw new InvalidOperationException(
            "Jwt:Secret en az 32 bayt olmalı. Üretimde user-secrets / ortam değişkeni (Jwt__Secret) ile verin.");
});

builder.Services.AddGvnCaching(cache => configuration.GetSection(CacheOptions.SectionName).Bind(cache));

var backgroundJobsEnabled = configuration.GetValue("BackgroundJobs:Enabled", true);
if (backgroundJobsEnabled)
    builder.Services.AddGvnBackgroundJobs(hf => ConfigureHangfire(hf, configuration));

builder.Services.AddGvnSwagger(
    "LifeQuest API", "v1",
    "Gerçek hayatı oyunlaştıran kişisel keşif ve gelişim platformu.");

// ── LifeQuest ────────────────────────────────────────────────────────────────
builder.Services.AddLifeQuestApplication(configuration);
builder.Services.LoadModules(InfrastructureAssembly.Instance);
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddHealthChecks().AddDbContextCheck<LifeQuestDbContext>("database");

var app = builder.Build();

await app.Services.InitializeLifeQuestDatabaseAsync();

// Ters proxy (nginx) arkasında: gerçek istemci IP'si rate limit için X-Forwarded-For'dan okunur.
// Yalnızca API doğrudan internete açık olmadığında (compose "app" profili) etkinleştirilmelidir.
if (configuration.GetValue("ReverseProxy:Enabled", false))
{
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

app.UseGvnCorrelationId();
app.UseMiddleware<CorrelationIdLogContextMiddleware>();
app.UseSerilogRequestLogging();
app.UseGvnExceptionHandling();

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AccountStatusMiddleware>();
app.UseAuthorization();

app.UseModules();

// API dokümantasyonu geliştirmede her zaman, test ortamında ApiDocs:Enabled ile açılır; üretimde kapalı kalır.
if (app.Environment.IsDevelopment() || configuration.GetValue("ApiDocs:Enabled", false))
    app.UseGvnSwagger();

if (app.Environment.IsDevelopment() && backgroundJobsEnabled)
    app.UseGvnHangfireDashboard();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program
{
    /// <summary>
    /// Hangfire deposu: <c>Hangfire:StorageProvider</c> = <c>PostgreSql</c> (üretim/test; yeniden başlatmada işler
    /// kaybolmaz, birden çok API örneği aynı kuyruğu paylaşır) veya <c>InMemory</c> (geliştirme). PostgreSQL deposu
    /// uygulama veritabanında ayrı <c>hangfire</c> şemasında tutulur ve açılışta gerekirse oluşturulur.
    /// </summary>
    private static void ConfigureHangfire(HangfireOptions options, IConfiguration configuration)
    {
        configuration.GetSection(HangfireOptions.SectionName).Bind(options);

        var provider = configuration.GetValue("Hangfire:StorageProvider", "InMemory");
        if (!string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            options.Storage = HangfireStorageMode.InMemory;
            return;
        }

        var connectionString = configuration.GetConnectionString(PersistenceOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Hangfire:StorageProvider=PostgreSql için ConnectionStrings:{PersistenceOptions.ConnectionStringName} gerekli.");

        options.Storage = HangfireStorageMode.Custom;
        options.ConfigureStorage = global => global.UsePostgreSqlStorage(
            npgsql => npgsql.UseNpgsqlConnection(connectionString),
            new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(5),
                InvisibilityTimeout = TimeSpan.FromMinutes(30)
            });
    }
}
