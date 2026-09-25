using Gvn.GvnFramework.Domain.Aggregates;
using Gvn.GvnFramework.Domain.Entities;
using Gvn.GvnFramework.EntityFramewokCore.Context;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Community;
using LifeQuest.Domain.Experiments;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence;

/// <summary>
/// Framework <see cref="GvnDbContext{TContext}"/>: audit alanları, soft-delete filtresi ve commit sonrası
/// domain event yayını buradan gelir.
/// </summary>
public sealed class LifeQuestDbContext(DbContextOptions<LifeQuestDbContext> options, IMediator? mediator = null)
    : GvnDbContext<LifeQuestDbContext>(options, mediator)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<InterestRelation> InterestRelations => Set<InterestRelation>();
    public DbSet<QuestTemplate> QuestTemplates => Set<QuestTemplate>();
    public DbSet<UserQuest> UserQuests => Set<UserQuest>();
    public DbSet<PlayerProgress> PlayerProgress => Set<PlayerProgress>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
    public DbSet<WeeklySummary> WeeklySummaries => Set<WeeklySummary>();
    public DbSet<AdminAuditEntry> AdminAuditEntries => Set<AdminAuditEntry>();
    public DbSet<RecommendationSettings> RecommendationSettings => Set<RecommendationSettings>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<QuestIdea> QuestIdeas => Set<QuestIdea>();
    public DbSet<SavedQuest> SavedQuests => Set<SavedQuest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LifeQuestDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => !t.IsOwned()).ToList())
        {
            var clrType = entityType.ClrType;
            if (!typeof(Entity).IsAssignableFrom(clrType))
                continue;

            // Id'ler domain'de üretilir (Guid.NewGuid). Aksi halde EF, navigation üzerinden eklenen yeni
            // child entity'leri "var olan" sanıp INSERT yerine UPDATE üretir.
            modelBuilder.Entity(clrType).Property(nameof(Entity.Id)).ValueGeneratedNever();

            if (typeof(AggregateRoot).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Ignore(nameof(AggregateRoot.DomainEvents));
        }

        // Soft-delete query filter'ları entity'ler kaydedildikten sonra uygulanmalı.
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        StoreAsString<LifeCategory>(configurationBuilder);
        StoreAsString<CostBand>(configurationBuilder);
        StoreAsString<DiscoveryRadius>(configurationBuilder);
        StoreAsString<InterestSource>(configurationBuilder);
        StoreAsString<InterestRelationType>(configurationBuilder);
        StoreAsString<RelationSource>(configurationBuilder);
        StoreAsString<QuestType>(configurationBuilder);
        StoreAsString<Difficulty>(configurationBuilder);
        StoreAsString<SafetyLevel>(configurationBuilder);
        StoreAsString<QuestStatus>(configurationBuilder);
        StoreAsString<QuestSource>(configurationBuilder);
        StoreAsString<SkipReason>(configurationBuilder);
        StoreAsString<FeedbackPreference>(configurationBuilder);
        StoreAsString<XpSourceType>(configurationBuilder);
        StoreAsString<PhysicalEffort>(configurationBuilder);
        StoreAsString<NotificationPreference>(configurationBuilder);
        StoreAsString<NarrationSource>(configurationBuilder);
        StoreAsString<EditorialSource>(configurationBuilder);
        StoreAsString<AdminAction>(configurationBuilder);
        StoreAsString<AdminTargetType>(configurationBuilder);
        StoreAsString<ExperimentStatus>(configurationBuilder);
        StoreAsString<ExperimentOutcome>(configurationBuilder);
        StoreAsString<ExperimentVariant>(configurationBuilder);
        StoreAsString<IdeaStatus>(configurationBuilder);
    }

    private static void StoreAsString<TEnum>(ModelConfigurationBuilder builder) where TEnum : struct, Enum
        => builder.Properties<TEnum>().HaveConversion<string>().HaveMaxLength(32);
}
