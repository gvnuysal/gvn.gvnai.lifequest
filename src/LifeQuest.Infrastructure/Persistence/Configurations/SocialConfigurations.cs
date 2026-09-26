using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class QuestPartyConfiguration : IEntityTypeConfiguration<QuestParty>
{
    public void Configure(EntityTypeBuilder<QuestParty> builder)
    {
        builder.ToTable("quest_parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuestTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InviteCode).HasMaxLength(QuestParty.InviteCodeLength).IsRequired();
        builder.HasIndex(x => x.InviteCode).IsUnique();
        builder.HasIndex(x => x.HostUserId);
        builder.HasRowVersion();

        builder.HasMany(x => x.Members).WithOne().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PartyMemberConfiguration : IEntityTypeConfiguration<PartyMember>
{
    public void Configure(EntityTypeBuilder<PartyMember> builder)
    {
        builder.ToTable("quest_party_members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        // Hesap silinince üyelik de silinir; parti diğer üyelerle sürer.
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.PartyId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserQuestId).IsUnique();
    }
}
