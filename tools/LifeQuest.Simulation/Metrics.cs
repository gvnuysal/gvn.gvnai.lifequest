namespace LifeQuest.Simulation;

internal sealed record ScenarioMetrics(
    Scenario Scenario,
    int Users,
    double Precision,
    double EarlyPrecision,
    double LatePrecision,
    double Acceptance,
    double NorthStar,
    double AverageRating,
    double CategoryCoverage,
    double CatalogCoverage,
    double Repetition,
    double IntraListDiversity,
    double HiddenDiscovery,
    double FirstDayShortAndRelevant,
    double AboveAbilityShare,
    double ShortfallDays,
    double ExplorationAcceptance,
    double ExplorationAffinity,
    double[] DailyPrecision,
    double NorthStarSe,
    double PrecisionSe,
    double HiddenDiscoverySe)
{
    public const double RelevantAffinity = 0.5;

    /// <summary>Kullanıcılar arası ortalamanın standart hatası (±1.96·SE ≈ %95 güven aralığı).</summary>
    public static double StandardError(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        var mean = list.Average();
        var variance = list.Sum(v => (v - mean) * (v - mean)) / (list.Count - 1);
        return Math.Sqrt(variance / list.Count);
    }

    public static ScenarioMetrics From(Scenario scenario, IReadOnlyList<UserRun> runs, int days)
    {
        var offers = runs.SelectMany(r => r.Offers).ToList();
        var weeks = days / 7.0;

        double Share<T>(IEnumerable<T> items, Func<T, bool> predicate)
        {
            var list = items.ToList();
            return list.Count == 0 ? 0 : list.Count(predicate) / (double)list.Count;
        }

        var daily = Enumerable.Range(0, days)
            .Select(d => Share(offers.Where(o => o.Day == d), o => o.Affinity >= RelevantAffinity))
            .ToArray();

        var repetition = runs.SelectMany(r => r.Offers.Select(o =>
                r.Offers.Any(p => p.Code == o.Code && p.Day < o.Day && p.Day >= o.Day - 7)))
            .ToList();

        var exploration = offers.Where(o => o.IsExploration).ToList();
        var ratings = runs.SelectMany(r => r.Completions).Where(c => c.Rating is not null).Select(c => c.Rating!.Value).ToList();
        var hiddenTotal = runs.Sum(r => r.Persona.Hidden.Length);

        return new ScenarioMetrics(
            scenario,
            runs.Count,
            Share(offers, o => o.Affinity >= RelevantAffinity),
            Share(offers.Where(o => o.Day < 5), o => o.Affinity >= RelevantAffinity),
            Share(offers.Where(o => o.Day >= days - 5), o => o.Affinity >= RelevantAffinity),
            Share(offers, o => o.Accepted),
            runs.Average(r => r.Completions.Count(c => c.Rating is null or >= 4) / weeks),
            ratings.Count == 0 ? 0 : ratings.Average(),
            runs.Average(r => r.Completions.Select(c => c.Category).Distinct().Count()),
            runs.Average(r => r.Offers.Select(o => o.Code).Distinct().Count()),
            repetition.Count == 0 ? 0 : repetition.Count(x => x) / (double)repetition.Count,
            runs.Average(r => r.Offers.GroupBy(o => o.Day).Average(g => g.Select(o => o.Category).Distinct().Count())),
            hiddenTotal == 0 ? 0 : runs.Sum(r => r.HiddenInterestsDiscovered) / (double)hiddenTotal,
            Share(runs, r => r.Offers.FirstOrDefault(o => o.Day == 0 && o.Slot == 0) is { IsShort: true, Affinity: >= RelevantAffinity }),
            Share(offers, o => o.AboveAbility),
            runs.Average(r => r.ShortfallDays),
            Share(exploration, o => o.Accepted),
            exploration.Count == 0 ? 0 : exploration.Average(o => o.Affinity),
            daily,
            StandardError(runs.Select(r => r.Completions.Count(c => c.Rating is null or >= 4) / weeks)),
            StandardError(runs.Select(r => r.Offers.Count == 0 ? 0 : r.Offers.Count(o => o.Affinity >= RelevantAffinity) / (double)r.Offers.Count)),
            StandardError(runs.Select(r => r.Persona.Hidden.Length == 0 ? 0 : r.HiddenInterestsDiscovered / (double)r.Persona.Hidden.Length)));
    }
}
