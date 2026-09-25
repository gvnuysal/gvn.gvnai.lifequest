using System.Text.Json;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    public void Configure(EntityTypeBuilder<RecommendationSettings> builder)
    {
        builder.ToTable("recommendation_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UpdatedBy).HasMaxLength(254);
        builder.Property(x => x.Overrides)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, Json) ?? new Dictionary<string, double>(),
                new ValueComparer<Dictionary<string, double>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
                    v => new Dictionary<string, double>(v)));
        builder.HasRowVersion();
    }
}
