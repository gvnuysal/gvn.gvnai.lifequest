namespace LifeQuest.Simulation;

internal static partial class Report
{
    public static string Pct(double value) => $"%{value * 100:0}";

    public static void PrintTable(IEnumerable<ScenarioMetrics> metrics)
    {
        Console.WriteLine($"{"Senaryo",-34} {"İsabet",7} {"İlk5",6} {"Son5",6} {"Kabul",6} {"NS/hf",6} {"Puan",5} {"KatKap",6} {"Katalog",7} {"Tekrar",6} {"Çeşit",5} {"Gizli",6} {"Gün1",5} {"Efor>",6} {"Eksik",5} {"KeşKab",6} {"KeşİlG",6}");
        foreach (var m in metrics)
            Console.WriteLine($"{m.Scenario.Key + " " + m.Scenario.Name,-34} {Pct(m.Precision),7}±{m.PrecisionSe*100:0} {Pct(m.EarlyPrecision),6} {Pct(m.LatePrecision),6} {Pct(m.Acceptance),6} {m.NorthStar,6:0.00}±{m.NorthStarSe:0.00} {m.AverageRating,5:0.0} {m.CategoryCoverage,6:0.0} {m.CatalogCoverage,7:0.0} {Pct(m.Repetition),6} {m.IntraListDiversity,5:0.00} {Pct(m.HiddenDiscovery),6}±{m.HiddenDiscoverySe*100:0} {Pct(m.FirstDayShortAndRelevant),5} {Pct(m.AboveAbilityShare),6} {m.ShortfallDays,5:0.0} {Pct(m.ExplorationAcceptance),6} {m.ExplorationAffinity,6:0.00}");
    }

    public static void PrintPersonas(IEnumerable<(Persona Persona, ScenarioMetrics Metrics)> personas)
    {
        Console.WriteLine($"{"Persona (A)",-34} {"İsabet",7} {"İlk5",6} {"Son5",6} {"NS/hf",6} {"KatKap",6} {"Gizli",6} {"Eksik",5}");
        foreach (var (p, m) in personas)
            Console.WriteLine($"{p.Name,-34} {Pct(m.Precision),7} {Pct(m.EarlyPrecision),6} {Pct(m.LatePrecision),6} {m.NorthStar,6:0.00} {m.CategoryCoverage,6:0.0} {Pct(m.HiddenDiscovery),6} {m.ShortfallDays,5:0.0}");
    }

    public static partial void Write(string docsDir, ReportData data);
}

internal sealed record ReportData(
    SimulationCatalog Catalog,
    IReadOnlyList<ScenarioMetrics> Core,
    IReadOnlyList<ScenarioMetrics> Ablations,
    IReadOnlyList<ScenarioMetrics> Sparse,
    IReadOnlyList<ScenarioMetrics> Radius,
    ScenarioMetrics MobilityWith,
    ScenarioMetrics MobilityWithout,
    IReadOnlyList<(Persona Persona, ScenarioMetrics Metrics)> Personas,
    int Days,
    int Seeds);
