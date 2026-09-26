namespace LifeQuest.Domain.Recommendations;

public sealed record InterestEdge(Guid FromInterestId, Guid ToInterestId, double Weight, double Confidence);

public sealed record InterestInfo(Guid Id, string Code, string Name, string? NameEn = null);

/// <summary>
/// İlgi alanları arasındaki komşuluk. Benzer öğe tavsiyesinin ötesine geçip komşu ilgi alanlarına
/// (Kahve → Kafe Kültürü → Mimari → Fotoğrafçılık) kontrollü geçiş sağlar. Kenarlar çift yönlü okunur.
/// </summary>
public sealed class TasteGraph
{
    public static readonly TasteGraph Empty = new([], []);

    private readonly Dictionary<Guid, List<(Guid Neighbor, double Strength)>> _adjacency = [];
    private readonly Dictionary<Guid, InterestInfo> _interests;

    public TasteGraph(IEnumerable<InterestEdge> edges, IEnumerable<InterestInfo> interests)
    {
        _interests = interests.ToDictionary(i => i.Id);

        foreach (var edge in edges)
        {
            var strength = edge.Weight * edge.Confidence;
            Add(edge.FromInterestId, edge.ToInterestId, strength);
            Add(edge.ToInterestId, edge.FromInterestId, strength);
        }
    }

    public IReadOnlyList<(Guid Neighbor, double Strength)> Neighbors(Guid interestId)
        => _adjacency.TryGetValue(interestId, out var list) ? list : [];

    public Localization.LocalizedText NameOf(Guid interestId)
        => _interests.TryGetValue(interestId, out var info)
            ? Localization.LocalizedText.WithFallback(info.Name, info.NameEn)
            : new("bu alan", "this area");

    private void Add(Guid from, Guid to, double strength)
    {
        if (!_adjacency.TryGetValue(from, out var list))
            _adjacency[from] = list = [];

        var index = list.FindIndex(n => n.Neighbor == to);
        if (index < 0) list.Add((to, strength));
        else if (list[index].Strength < strength) list[index] = (to, strength);
    }
}
