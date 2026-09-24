using Gvn.GvnFramework.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    /// <summary>PostgreSQL xmin sistem kolonu üzerinden optimistic concurrency (ek kolon oluşturmaz).</summary>
    public static void HasRowVersion<T>(this EntityTypeBuilder<T> builder) where T : Entity
        => builder.Property<uint>("RowVersion").IsRowVersion();
}
