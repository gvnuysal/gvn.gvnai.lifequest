using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class AdminAuditEntryConfiguration : IEntityTypeConfiguration<AdminAuditEntry>
{
    public void Configure(EntityTypeBuilder<AdminAuditEntry> builder)
    {
        builder.ToTable("admin_audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorEmail).HasMaxLength(254).IsRequired();
        builder.Property(x => x.TargetLabel).HasMaxLength(254).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Details).HasColumnType("jsonb");

        // Hesap silinse de kayıt kalır: bilinçli olarak FK yok.
        builder.HasIndex(x => x.CreatedAt).IsDescending();
        builder.HasIndex(x => new { x.Action, x.CreatedAt });
    }
}

internal sealed class RecommendationSettingsConfiguration : IEntityTypeConfiguration<RecommendationSettings>
{
    public void Configure(EntityTypeBuilder<RecommendationSettings> builder)
    {
        builder.ToTable("recommendation_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UpdatedBy).HasMaxLength(254);
        builder.Property(x => x.Overrides).AsJsonb();
        builder.HasRowVersion();
    }
}

internal sealed class ExperimentConfiguration : IEntityTypeConfiguration<Domain.Experiments.Experiment>
{
    public void Configure(EntityTypeBuilder<Domain.Experiments.Experiment> builder)
    {
        builder.ToTable("experiments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Hypothesis).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(254).IsRequired();
        builder.Property(x => x.TreatmentOverrides).AsJsonb();

        // Aynı anda tek deney: eşzamanlı iki "başlat" isteğinden ikincisi veritabanında reddedilir.
        builder.HasIndex(x => x.Status).IsUnique().HasFilter("status = 'Running'").HasDatabaseName("ux_experiments_single_running");
        builder.HasRowVersion();
    }
}
