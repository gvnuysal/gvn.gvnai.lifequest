using Gvn.GvnFramework.Domain.Entities;
using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Recommendations;

/// <summary>
/// Admin'in panelden verdiği ağırlıklar. Tek satırdır ve yalnızca appsettings'ten farklı değerleri (override) tutar;
/// böylece "varsayılana dön" yalnızca anahtarı silmek demektir.
/// </summary>
public sealed class RecommendationSettings : Entity
{
    public static readonly Guid SingletonId = new("7e57a11d-0000-4000-8000-000000000001");

    public Dictionary<string, double> Overrides { get; private set; } = [];

    /// <summary>İstemcinin gördüğü sürüm; eşzamanlı iki admin düzenlemesinde ikincisi 409 alır.</summary>
    public int Revision { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    private RecommendationSettings() { }

    public static RecommendationSettings CreateEmpty() => new() { Id = SingletonId };

    /// <summary>Yeni değerleri uygular; config varsayılanına eşit bir değer override olarak saklanmaz.</summary>
    /// <returns>Değişen anahtarlar: önceki ve yeni etkin değer.</returns>
    public IReadOnlyList<WeightChange> Update(
        IReadOnlyDictionary<string, double> values, RecommendationWeights defaults, string updatedBy, DateTime nowUtc)
    {
        var changes = new List<WeightChange>();
        foreach (var (key, value) in values)
        {
            var field = RecommendationWeightCatalog.Get(key);
            var before = Overrides.TryGetValue(key, out var current) ? current : field.Get(defaults);
            var normalized = field.Normalize(value);
            if (Math.Abs(before - normalized) < 1e-9)
                continue;

            if (Math.Abs(field.Get(defaults) - normalized) < 1e-9)
                Overrides.Remove(key);
            else
                Overrides[key] = normalized;
            changes.Add(new WeightChange(key, before, normalized));
        }

        Touch(changes, updatedBy, nowUtc);
        return changes;
    }

    /// <param name="keys"><c>null</c> ise tüm override'lar silinir.</param>
    public IReadOnlyList<WeightChange> Reset(IReadOnlyCollection<string>? keys, RecommendationWeights defaults, string updatedBy, DateTime nowUtc)
    {
        var changes = Overrides
            .Where(o => keys is null || keys.Contains(o.Key))
            .Select(o => new WeightChange(o.Key, o.Value, RecommendationWeightCatalog.Get(o.Key).Get(defaults)))
            .ToList();
        foreach (var change in changes)
            Overrides.Remove(change.Key);

        Touch(changes, updatedBy, nowUtc);
        return changes;
    }

    private void Touch(IReadOnlyCollection<WeightChange> changes, string updatedBy, DateTime nowUtc)
    {
        if (changes.Count == 0)
            return;

        Overrides = new Dictionary<string, double>(Overrides);
        Revision++;
        UpdatedAt = nowUtc;
        UpdatedBy = updatedBy;
    }
}

public sealed record WeightChange(string Key, double Before, double After);

public interface IRecommendationSettingsRepository : IRepository<RecommendationSettings>
{
    Task<RecommendationSettings?> GetAsync(CancellationToken cancellationToken = default);
}
