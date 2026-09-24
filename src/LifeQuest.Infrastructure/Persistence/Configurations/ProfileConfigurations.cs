using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.City).HasMaxLength(80);
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.PrimitiveCollection(x => x.Goals);
        builder.Property(x => x.MaxPhysicalEffort).HasDefaultValue(PhysicalEffort.Vigorous).HasSentinel((PhysicalEffort)(-1));
        builder.Property(x => x.NotificationPreference).HasDefaultValue(NotificationPreference.WeeklySummary).HasSentinel((NotificationPreference)(-1));
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Interests).WithOne().HasForeignKey(x => x.UserProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Interests).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasRowVersion();
    }
}

internal sealed class UserInterestConfiguration : IEntityTypeConfiguration<UserInterest>
{
    public void Configure(EntityTypeBuilder<UserInterest> builder)
    {
        builder.ToTable("user_interests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserProfileId, x.InterestId }).IsUnique();
        builder.HasOne<Interest>().WithMany().HasForeignKey(x => x.InterestId).OnDelete(DeleteBehavior.Restrict);
    }
}
