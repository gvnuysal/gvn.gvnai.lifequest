using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Recommendations;
using LifeQuest.Infrastructure.Persistence.Seed;

namespace LifeQuest.Simulation;

/// <summary>Üretimdeki seed kataloğundan (100 template) bellek içi engine girdisi.</summary>
internal sealed class SimulationCatalog
{
    public IReadOnlyDictionary<string, Guid> InterestIds { get; }
    public IReadOnlyDictionary<Guid, string> InterestCodes { get; }
    public IReadOnlyList<QuestCandidate> Candidates { get; }
    public IReadOnlyList<QuestCandidate> StarterCards { get; }
    public TasteGraph Graph { get; }

    public SimulationCatalog()
    {
        var ids = CatalogSeedData.Interests.ToDictionary(i => i.Code, i => DeterministicGuid("interest:" + i.Code));
        InterestIds = ids;
        InterestCodes = ids.ToDictionary(x => x.Value, x => x.Key);

        var candidates = new List<QuestCandidate>();
        var starters = new List<QuestCandidate>();
        foreach (var seed in CatalogSeedData.Templates)
        {
            var spec = CatalogSeeder.ToSpec(seed, ids);
            if (CatalogSafetyRules.ValidateTemplate(spec).Count > 0)
                continue;

            var candidate = new QuestCandidate(
                DeterministicGuid("template:" + spec.Code), spec.Code, 1, spec.Title, spec.Description, spec.Type,
                spec.Difficulty, spec.Category, spec.SecondaryCategory, spec.MinMinutes, spec.MaxMinutes, spec.Cost,
                spec.DayParts, spec.RequiresCity, spec.RiskScore, spec.CooldownDays, spec.InterestIds, spec.IsOutdoor, spec.Effort);

            candidates.Add(candidate);
            if (spec.IsStarter) starters.Add(candidate);
        }

        Candidates = candidates;
        StarterCards = starters;
        Graph = new TasteGraph(
            CatalogSeedData.Relations.Select(r => new InterestEdge(ids[r.From], ids[r.To], r.Weight, 0.8)),
            CatalogSeedData.Interests.Select(i => new InterestInfo(ids[i.Code], i.Code, i.Name)));
    }

    public static Guid DeterministicGuid(string key)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }
}
