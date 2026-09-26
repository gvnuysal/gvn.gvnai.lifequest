using LifeQuest.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeQuest.Infrastructure.Persistence.Configurations;

internal sealed class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("interests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class InterestRelationConfiguration : IEntityTypeConfiguration<InterestRelation>
{
    public void Configure(EntityTypeBuilder<InterestRelation> builder)
    {
        builder.ToTable("interest_relations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.FromInterestId, x.ToInterestId }).IsUnique();
        builder.HasOne<Interest>().WithMany().HasForeignKey(x => x.FromInterestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Interest>().WithMany().HasForeignKey(x => x.ToInterestId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuestTemplateConfiguration : IEntityTypeConfiguration<QuestTemplate>
{
    public void Configure(EntityTypeBuilder<QuestTemplate> builder)
    {
        builder.ToTable("quest_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TitleEn).HasMaxLength(150);
        builder.Property(x => x.DescriptionEn).HasMaxLength(1000);
        builder.Property(x => x.DeletedBy).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Safety });
        builder.Property(x => x.Effort).HasDefaultValue(PhysicalEffort.Light).HasSentinel((PhysicalEffort)(-1));
        builder.Property(x => x.Source).HasDefaultValue(EditorialSource.Seed).HasSentinel((EditorialSource)(-1));
        builder.HasRowVersion();
    }
}

internal sealed class LocalPlaceConfiguration : IEntityTypeConfiguration<LifeQuest.Domain.RealWorld.LocalPlace>
{
    public void Configure(EntityTypeBuilder<LifeQuest.Domain.RealWorld.LocalPlace> builder)
    {
        builder.ToTable("local_places");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.City).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CityKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(200);
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.Property(x => x.CreatedByEmail).HasMaxLength(254).IsRequired();
        builder.PrimitiveCollection(x => x.TemplateIds);
        builder.HasRowVersion();

        // Öneri anında: şehir + tür + tarih aralığı.
        builder.HasIndex(x => new { x.CityKey, x.Kind, x.EndsAt });
    }
}
