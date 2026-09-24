using LifeQuest.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Infrastructure.Persistence.Seed;

/// <summary>
/// İdempotent katalog seed'i: yalnızca eksik kodları ekler, mevcut kayıtları değiştirmez
/// (template değişiklikleri ileride admin akışıyla ve Version artırılarak yapılmalı).
/// </summary>
internal sealed class CatalogSeeder(LifeQuestDbContext db, ILogger<CatalogSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var interests = await db.Interests.ToDictionaryAsync(i => i.Code, cancellationToken);
        foreach (var seed in CatalogSeedData.Interests.Where(s => !interests.ContainsKey(s.Code)))
        {
            var interest = Interest.Create(seed.Code, seed.Name, seed.Category);
            db.Interests.Add(interest);
            interests[seed.Code] = interest;
        }

        var existingRelations = (await db.InterestRelations
                .Select(r => new { r.FromInterestId, r.ToInterestId })
                .ToListAsync(cancellationToken))
            .Select(r => (r.FromInterestId, r.ToInterestId))
            .ToHashSet();

        var addedRelations = 0;
        foreach (var seed in CatalogSeedData.Relations)
        {
            var from = interests[seed.From].Id;
            var to = interests[seed.To].Id;
            if (!existingRelations.Add((from, to)))
                continue;

            db.InterestRelations.Add(InterestRelation.Create(from, to, seed.Type, seed.Weight, 0.8, RelationSource.Editorial));
            addedRelations++;
        }

        var templateCodes = await db.QuestTemplates.IgnoreQueryFilters().Select(t => t.Code).ToListAsync(cancellationToken);
        var addedTemplates = 0;
        foreach (var seed in CatalogSeedData.Templates.Where(s => !templateCodes.Contains(s.Code)))
        {
            db.QuestTemplates.Add(QuestTemplate.Create(
                seed.Code, seed.Title, seed.Description, seed.Type, seed.Difficulty,
                seed.Category, seed.Secondary, seed.MinMinutes, seed.MaxMinutes, seed.Cost, seed.DayParts,
                seed.RequiresCity, seed.IsOutdoor, seed.CooldownDays, seed.Risk,
                seed.Interests.Select(code => interests[code].Id)));
            addedTemplates++;
        }

        var saved = await db.SaveChangesAsync(cancellationToken);
        if (saved > 0)
            logger.LogInformation(
                "Catalog seeded: {Relations} relations, {Templates} templates ({Rows} rows)", addedRelations, addedTemplates, saved);
    }
}
