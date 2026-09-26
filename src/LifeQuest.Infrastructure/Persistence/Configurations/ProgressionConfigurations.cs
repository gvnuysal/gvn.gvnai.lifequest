using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class PlayerProgressConfiguration : IEntityTypeConfiguration<PlayerProgress>
{
    public void Configure(EntityTypeBuilder<PlayerProgress> builder)
    {
        builder.ToTable("player_progress");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Categories).WithOne().HasForeignKey(x => x.PlayerProgressId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Achievements).WithOne().HasForeignKey(x => x.PlayerProgressId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Achievements).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasRowVersion();
    }
}

internal sealed class CategoryProgressConfiguration : IEntityTypeConfiguration<CategoryProgress>
{
    public void Configure(EntityTypeBuilder<CategoryProgress> builder)
    {
        builder.ToTable("category_progress");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PlayerProgressId, x.Category }).IsUnique();
    }
}

internal sealed class UnlockedAchievementConfiguration : IEntityTypeConfiguration<UnlockedAchievement>
{
    public void Configure(EntityTypeBuilder<UnlockedAchievement> builder)
    {
        builder.ToTable("user_achievements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.PlayerProgressId, x.Code }).IsUnique();
    }
}

internal sealed class XpTransactionConfiguration : IEntityTypeConfiguration<XpTransaction>
{
    public void Configure(EntityTypeBuilder<XpTransaction> builder)
    {
        builder.ToTable("xp_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DescriptionEn).HasMaxLength(150);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Çift XP'ye karşı veritabanı seviyesinde son savunma hattı.
        builder.HasIndex(x => new { x.SourceType, x.SourceId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
