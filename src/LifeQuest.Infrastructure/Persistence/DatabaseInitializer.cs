using LifeQuest.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    }
}
