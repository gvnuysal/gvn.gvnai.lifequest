using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class WeeklySummaryConfiguration : IEntityTypeConfiguration<WeeklySummary>
{
    public void Configure(EntityTypeBuilder<WeeklySummary> builder)
    {
        builder.ToTable("weekly_summaries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(400).IsRequired();
        builder.PrimitiveCollection(x => x.NewCategories);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Job tekrar çalışsa da aynı hafta için tek özet.
        builder.HasIndex(x => new { x.UserId, x.WeekStart }).IsUnique();
    }
}

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Endpoint).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.P256dh).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Auth).HasMaxLength(100).IsRequired();
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Bir tarayıcı aboneliği tek hesaba aittir; başka hesapla girişte devredilir.
        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
