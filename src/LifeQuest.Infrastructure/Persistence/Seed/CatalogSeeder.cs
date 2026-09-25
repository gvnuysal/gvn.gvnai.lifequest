using LifeQuest.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Infrastructure.Persistence.Seed;

/// <summary>
/// Editoryal katalog senkronizasyonu (idempotent upsert):
/// <list type="bullet">
/// <item>Eksik ilgi alanı, ilişki ve template'ler eklenir.</item>
/// <item>Mevcut template'ler seed tanımına göre güncellenir; değişen template'in <c>Version</c>'ı artar.
/// Kullanıcılara verilmiş quest'ler snapshot olduğu için etkilenmez.</item>
/// <item><see cref="CatalogSafetyRules"/> kontrol listesini geçemeyen template <see cref="SafetyLevel.NeedsReview"/>
/// olarak işaretlenir ve önerilmez (çalışma zamanı koruması; CI'da katalog testleri aynı kuralı uygular).</item>
/// </list>
/// </summary>
internal sealed class CatalogSeeder(LifeQuestDbContext db, ILogger<CatalogSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var interests = await SeedInterestsAsync(cancellationToken);
        var addedRelations = await SeedRelationsAsync(interests, cancellationToken);
        var (added, updated, flagged) = await SeedTemplatesAsync(interests, cancellationToken);

        var saved = await db.SaveChangesAsync(cancellationToken);
        if (saved > 0)
            logger.LogInformation(
                "Catalog synced: {Relations} relations added, {Added} templates added, {Updated} updated, {Flagged} flagged for review",
                addedRelations, added, updated, flagged);
    }

    public static QuestTemplateSpec ToSpec(CatalogSeedData.TemplateSeed seed, IReadOnlyDictionary<string, Guid> interestIds)
        => new(
            seed.Code, seed.Title, seed.Description, seed.Type, seed.Difficulty, seed.Category, seed.Secondary,
            seed.MinMinutes, seed.MaxMinutes, seed.Cost, seed.DayParts, seed.RequiresCity, seed.IsOutdoor,
            seed.CooldownDays, seed.Risk, seed.Interests.Select(code => interestIds[code]).ToList(),
            seed.Effort, seed.Starter);

    private async Task<Dictionary<string, Guid>> SeedInterestsAsync(CancellationToken cancellationToken)
    {
        var interests = await db.Interests.ToDictionaryAsync(i => i.Code, cancellationToken);
        foreach (var seed in CatalogSeedData.Interests.Where(s => !interests.ContainsKey(s.Code)))
        {
            var interest = Interest.Create(seed.Code, seed.Name, seed.Category);
            db.Interests.Add(interest);
            interests[seed.Code] = interest;
        }

        return interests.ToDictionary(x => x.Key, x => x.Value.Id);
    }

    private async Task<int> SeedRelationsAsync(IReadOnlyDictionary<string, Guid> interests, CancellationToken cancellationToken)
    {
        var existing = (await db.InterestRelations
                .Select(r => new { r.FromInterestId, r.ToInterestId })
                .ToListAsync(cancellationToken))
            .Select(r => (r.FromInterestId, r.ToInterestId))
            .ToHashSet();

        var added = 0;
        foreach (var seed in CatalogSeedData.Relations)
        {
            var pair = (interests[seed.From], interests[seed.To]);
            if (!existing.Add(pair))
                continue;

            db.InterestRelations.Add(InterestRelation.Create(pair.Item1, pair.Item2, seed.Type, seed.Weight, 0.8, RelationSource.Editorial));
            added++;
        }

        return added;
    }

    private async Task<(int Added, int Updated, int Flagged)> SeedTemplatesAsync(
        IReadOnlyDictionary<string, Guid> interests, CancellationToken cancellationToken)
    {
        var templates = await db.QuestTemplates.IgnoreQueryFilters().ToDictionaryAsync(t => t.Code, cancellationToken);
        int added = 0, updated = 0, flagged = 0;

        foreach (var seed in CatalogSeedData.Templates)
        {
            var spec = ToSpec(seed, interests);
            var violations = CatalogSafetyRules.ValidateTemplate(spec);
            var safety = violations.Count == 0 ? SafetyLevel.Safe : SafetyLevel.NeedsReview;

            if (violations.Count > 0)
            {
                flagged++;
                logger.LogWarning("Template {Code} failed the editorial safety checklist: {Violations}",
                    seed.Code, string.Join(" | ", violations));
            }

            if (!templates.TryGetValue(seed.Code, out var template))
            {
                db.QuestTemplates.Add(QuestTemplate.Create(spec, safety));
                added++;
                continue;
            }

            // Admin'in düzenlediği veya güvenlik kararını verdiği template artık seed ile senkronlanmaz.
            if (template.Source == EditorialSource.Admin)
                continue;

            // Editoryal olarak engellenmiş (Blocked) bir template seed ile yeniden açılmaz.
            if (template.Safety != SafetyLevel.Blocked && template.Safety != safety)
                template.SetSafety(safety);

            if (template.ApplyEditorial(spec))
                updated++;
        }

        return (added, updated, flagged);
    }
}
