using LifeQuest.Application.Identity;
using LifeQuest.Infrastructure.Admin;
using LifeQuest.Domain.Identity;
using LifeQuest.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Geliştirme ortamı için. Üretimde migration'lar pipeline'da ayrı adım olarak uygulanmalı.</summary>
    public bool MigrateOnStartup { get; set; }

    public bool SeedCatalog { get; set; } = true;
}


public static class DatabaseInitializer
{
    public static async Task InitializeLifeQuestDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var options = services.GetRequiredService<IConfiguration>()
            .GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        await using var scope = services.CreateAsyncScope();

        if (options.MigrateOnStartup)
            await scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>().Database.MigrateAsync(cancellationToken);

        if (options.SeedCatalog)
            await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(cancellationToken);

        var admin = services.GetRequiredService<IConfiguration>()
            .GetSection(AdminOptions.SectionName).Get<AdminOptions>() ?? new AdminOptions();
        await PromoteAdminsAsync(scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>(), admin, cancellationToken);

        var experiments = services.GetRequiredService<IConfiguration>()
            .GetSection(ExperimentsOptions.SectionName).Get<ExperimentsOptions>() ?? new ExperimentsOptions();
        await ExperimentAutoStart.RunAsync(
            scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>(),
            experiments.AutoStartPreset,
            services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime,
            services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ExperimentAutoStart)),
            cancellationToken);
    }

    private static async Task PromoteAdminsAsync(LifeQuestDbContext db, AdminOptions options, CancellationToken cancellationToken)
    {
        var emails = options.BootstrapEmails.Select(UserAccount.NormalizeEmail).ToList();
        if (emails.Count == 0)
            return;

        var accounts = await db.UserAccounts.Where(a => emails.Contains(a.Email)).ToListAsync(cancellationToken);
        if (accounts.Count(a => a.GrantRole(UserRoles.Admin)) > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
