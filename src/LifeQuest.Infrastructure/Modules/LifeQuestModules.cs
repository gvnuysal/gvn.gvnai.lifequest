using Gvn.GvnFramework.BackgroundJobs.Abstractions;
using Gvn.GvnFramework.DepedencyInjection.Extensions;
using Gvn.GvnFramework.Domain.Repositories;
using Gvn.GvnFramework.EntityFramewokCore.UnitOfWork;
using Gvn.GvnFramework.Modularity.Abstractions;
using Hangfire;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Infrastructure.Catalog;
using LifeQuest.Infrastructure.Jobs;
using LifeQuest.Infrastructure.Persistence;
using LifeQuest.Infrastructure.Persistence.Repositories;
using LifeQuest.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeQuest.Infrastructure.Modules;

// Modular monolith: her modül kendi servislerini framework IModule sözleşmesiyle kaydeder
// ve API'de ModuleLoader.LoadModules(...) ile assembly taranarak yüklenir.

public sealed class PersistenceModule : IModule
{
    public string Name => "Persistence";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<LifeQuestDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(PersistenceOptions.ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{PersistenceOptions.ConnectionStringName} tanımlı değil.");
            PersistenceOptions.Configure(options, connectionString);
        });

        // Framework UnitOfWork + framework Decorate ile hata çevirici decorator.
        services.AddScoped<IUnitOfWork, UnitOfWork<LifeQuestDbContext>>();
        services.Decorate<IUnitOfWork, ExceptionTranslatingUnitOfWork>();
    }

    public void Configure(IApplicationBuilder app) { }
}

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }

    public void Configure(IApplicationBuilder app) { }
}

public sealed class ProfileModule : IModule
{
    public string Name => "Profile";

    public void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IUserProfileRepository, UserProfileRepository>();

    public void Configure(IApplicationBuilder app) { }
}

public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IQuestCatalog, CachedQuestCatalog>();
        services.AddScoped<CatalogSeeder>();
    }

    public void Configure(IApplicationBuilder app) { }
}

public sealed class ProgressionModule : IModule
{
    public string Name => "Progression";

    public void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPlayerProgressRepository, PlayerProgressRepository>();

    public void Configure(IApplicationBuilder app) { }
}

public sealed class QuestModule : IModule
{
    public string Name => "Quest";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUserQuestRepository, UserQuestRepository>();
        services.AddScoped<ExpireStaleQuestsJob>();
        services.AddScoped<DailyQuestGenerationJob>();
    }

    /// <summary>Hangfire etkinse periyodik job'ları framework IBackgroundJobService üzerinden kaydeder.</summary>
    public void Configure(IApplicationBuilder app)
    {
        // Statik RecurringJob API'si JobStorage.Current'a ihtiyaç duyar; çözümlemek onu başlatır.
        if (app.ApplicationServices.GetService<JobStorage>() is null)
            return;

        using var scope = app.ApplicationServices.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();

        // Framework sözleşmesi Expression<Action<T>> alır; Hangfire dönen Task'ı kendisi bekler.
#pragma warning disable CS4014
        jobs.AddOrUpdateRecurring<ExpireStaleQuestsJob>(
            ExpireStaleQuestsJob.JobId, job => job.ExecuteAsync(CancellationToken.None), ExpireStaleQuestsJob.Cron);
        jobs.AddOrUpdateRecurring<DailyQuestGenerationJob>(
            DailyQuestGenerationJob.JobId, job => job.ExecuteAsync(CancellationToken.None), DailyQuestGenerationJob.Cron);
#pragma warning restore CS4014
    }
}
