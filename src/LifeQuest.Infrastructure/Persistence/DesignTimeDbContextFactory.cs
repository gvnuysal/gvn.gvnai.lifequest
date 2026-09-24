using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LifeQuest.Infrastructure.Persistence;

/// <summary><c>dotnet ef</c> için. Bağlantı: LIFEQUEST_DB ortam değişkeni veya yerel docker-compose varsayılanı.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LifeQuestDbContext>
{
    public LifeQuestDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LIFEQUEST_DB")
            ?? "Host=localhost;Port=5433;Database=lifequest;Username=lifequest;Password=lifequest";

        var options = new DbContextOptionsBuilder<LifeQuestDbContext>();
        PersistenceOptions.Configure(options, connectionString);
        return new LifeQuestDbContext(options.Options);
    }
}

public static class PersistenceOptions
{
    public const string ConnectionStringName = "LifeQuest";

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
        => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}
