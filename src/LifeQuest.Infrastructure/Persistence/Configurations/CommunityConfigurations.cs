using LifeQuest.Domain.Community;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class QuestIdeaConfiguration : IEntityTypeConfiguration<QuestIdea>
{
    public void Configure(EntityTypeBuilder<QuestIdea> builder)
    {
        builder.ToTable("quest_ideas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ReviewNote).HasMaxLength(300);
        builder.Property(x => x.ReviewedBy).HasMaxLength(254);
        builder.PrimitiveCollection(x => x.Flags);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.SubmittedAt });
        builder.HasIndex(x => new { x.UserId, x.SubmittedAt });
        builder.HasRowVersion();
    }
}

internal sealed class SavedQuestConfiguration : IEntityTypeConfiguration<SavedQuest>
{
    public void Configure(EntityTypeBuilder<SavedQuest> builder)
    {
        builder.ToTable("saved_quests");
        builder.HasKey(x => x.Id);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.TemplateId }).IsUnique();
    }
}
