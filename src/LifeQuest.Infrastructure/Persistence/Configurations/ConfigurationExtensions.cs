using System.Text.Json;
using Gvn.GvnFramework.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    /// <summary>PostgreSQL xmin sistem kolonu üzerinden optimistic concurrency (ek kolon oluşturmaz).</summary>
    public static void HasRowVersion<T>(this EntityTypeBuilder<T> builder) where T : Entity
        => builder.Property<uint>("RowVersion").IsRowVersion();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    /// <summary>Anahtar → sayı sözlüğünü jsonb kolonunda saklar (ağırlık override'ları).</summary>
    public static PropertyBuilder<Dictionary<string, double>> AsJsonb(this PropertyBuilder<Dictionary<string, double>> property)
        => property
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, Json) ?? new Dictionary<string, double>(),
                new ValueComparer<Dictionary<string, double>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
                    v => new Dictionary<string, double>(v)));
}
