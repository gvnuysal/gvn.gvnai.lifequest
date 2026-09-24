using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class UserQuestConfiguration : IEntityTypeConfiguration<UserQuest>
{
    public void Configure(EntityTypeBuilder<UserQuest> builder)
    {
        builder.ToTable("user_quests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TemplateCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Explanation).HasMaxLength(500).IsRequired();
        builder.PrimitiveCollection(x => x.InterestIds);
        builder.PrimitiveCollection(x => x.ReasonCodes);

        builder.ComplexProperty(x => x.Reward, reward =>
        {
            reward.Property(r => r.NoveltyMultiplier).HasPrecision(4, 2);
        });
        builder.ComplexProperty(x => x.Score);

        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Template'e FK bilinçli olarak yok: UserQuest bir snapshot'tır, template silinse/değişse de geçmiş kalır.
        builder.HasIndex(x => x.TemplateId);
        builder.HasIndex(x => new { x.UserId, x.Status, x.ExpiresAt });
        builder.HasIndex(x => new { x.UserId, x.OfferDate, x.Source });
        builder.HasIndex(x => new { x.Status, x.ExpiresAt });

        // Aynı gün için günlük öneri tek kez üretilir (job + kullanıcı isteği yarışı dahil).
        builder.HasIndex(x => new { x.UserId, x.OfferDate, x.Slot })
            .IsUnique()
            .HasFilter("source = 'Daily'")
            .HasDatabaseName("ux_user_quests_daily_slot");

        builder.Property(x => x.Effort).HasDefaultValue(Domain.Catalog.PhysicalEffort.Light).HasSentinel((Domain.Catalog.PhysicalEffort)(-1));
        builder.Property(x => x.NarrationSource).HasDefaultValue(NarrationSource.Template).HasSentinel((NarrationSource)0);

        builder.HasRowVersion();
    }
}
