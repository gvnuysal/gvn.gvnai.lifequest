using System.Globalization;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Recommendations;
using LifeQuest.Simulation;

// Kullanım: dotnet run --project tools/LifeQuest.Simulation [-- --docs docs]
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
const int Days = 30;
const int SeedsPerPersona = 20;

var docsDir = args.SkipWhile(a => a != "--docs").Skip(1).FirstOrDefault();
var catalog = new SimulationCatalog();
var simulator = new Simulator(catalog);
var production = new RecommendationWeights();

// Yalnızca ilgi skoru: yenilik, bağlam, hedef, çeşitlilik, geri bildirim ve cezalar kapalı; keşif slotu yok.
var interestOnly = new RecommendationWeights
{
    NoveltyChill = 0, NoveltyExplore = 0, NoveltySurpriseMe = 0,
    Context = 0, GoalFit = 0, Diversity = 0, FeedbackFit = 0,
    Repetition = 0, Friction = 0, Risk = 0,
    ExplorationMaxInterest = -1
};

Scenario[] core =
[
    new("V0", "İlk sürüm (analiz öncesi)", production with
    {
        GuidedExploration = false, ExplorationRateExplore = 1.0, IgnoredOfferWindowDays = 3
    }),
    new("A", "Tam motor + cold start kartları", production),
    new("B", "Tam motor, kartsız", production, StarterCards: false),
    new("C", "Öğrenme kapalı", production, Learning: false, StarterCards: false),
    new("D", "Yalnızca ilgi skoru (greedy)", interestOnly)
];

Scenario[] radius =
[
    new("R-Chill", "Herkes Sakin", production, RadiusOverride: DiscoveryRadius.Chill),
    new("R-Explore", "Herkes Dengeli", production, RadiusOverride: DiscoveryRadius.Explore),
    new("R-Surprise", "Herkes Şaşırt Beni", production, RadiusOverride: DiscoveryRadius.SurpriseMe)
];

// Ablasyon: tam motordan tek bir bileşen çıkarıldığında ne değişiyor?
Scenario[] ablations =
[
    new("X-Rep", "A − tekrar cezası", production with { Repetition = 0 }),
    new("X-Nov", "A − yenilik", production with { NoveltyChill = 0, NoveltyExplore = 0, NoveltySurpriseMe = 0 }),
    new("X-Div", "A − çeşitlilik", production with { Diversity = 0 }),
    new("X-Exp", "A − keşif slotu", production with { ExplorationMaxInterest = -1 }),
    new("X-Ign3", "A, eski tekrar penceresi (3 gün)", production with { IgnoredOfferWindowDays = 3, IgnoredOfferPenalty = 0.3 })
];

// Cold start: kullanıcı onboarding'de yalnızca tek ilgi beyan ederse kartlar işe yarıyor mu?
Scenario[] sparse =
[
    new("S-A", "Tek ilgi beyanı + kartlar", production, SparseDeclaration: true),
    new("S-B", "Tek ilgi beyanı, kartsız", production, StarterCards: false, SparseDeclaration: true)
];

var effortOff = new Scenario("E", "Efor sınırı beyan edilmedi", production, DeclareEffortLimit: false);

IReadOnlyList<UserRun> RunAll(Scenario scenario, IEnumerable<Persona>? personas = null)
    => (personas ?? Persona.All)
        .SelectMany(p => Enumerable.Range(1, SeedsPerPersona).Select(seed => simulator.Run(p, scenario, seed, Days)))
        .ToList();

var coreRuns = core.ToDictionary(s => s, s => RunAll(s));
var coreMetrics = core.Select(s => ScenarioMetrics.From(s, coreRuns[s], Days)).ToList();
var radiusMetrics = radius.Select(s => ScenarioMetrics.From(s, RunAll(s), Days)).ToList();
var ablationMetrics = ablations.Select(s => ScenarioMetrics.From(s, RunAll(s), Days)).ToList();
var sparseMetrics = sparse.Select(s => ScenarioMetrics.From(s, RunAll(s), Days)).ToList();

var mobility = Persona.All.Where(p => p.Key == "mobility").ToList();
var mobilityWith = ScenarioMetrics.From(core[0], RunAll(core[0], mobility), Days);
var mobilityWithout = ScenarioMetrics.From(effortOff, RunAll(effortOff, mobility), Days);

var perPersona = Persona.All
    .Select(p => (Persona: p, Metrics: ScenarioMetrics.From(core[0], coreRuns[core[0]].Where(r => r.Persona == p).ToList(), Days)))
    .ToList();

Console.WriteLine($"Katalog: {catalog.Candidates.Count} template, {catalog.StarterCards.Count} başlangıç kartı · {Persona.All.Count} persona × {SeedsPerPersona} tohum × {Days} gün");
Console.WriteLine();
Report.PrintTable(coreMetrics.Concat(ablationMetrics).Concat(sparseMetrics).Concat(radiusMetrics).Append(mobilityWith with { Scenario = mobilityWith.Scenario with { Name = "Hareket kısıtlı · efor beyanlı" } })
    .Append(mobilityWithout with { Scenario = mobilityWithout.Scenario with { Name = "Hareket kısıtlı · efor beyansız" } }));
Console.WriteLine();
Report.PrintPersonas(perPersona);

if (docsDir is not null)
{
    Report.Write(docsDir, new ReportData(catalog, coreMetrics, ablationMetrics, sparseMetrics, radiusMetrics,
        mobilityWith, mobilityWithout, perPersona, Days, SeedsPerPersona));
    Console.WriteLine($"\nRapor yazıldı: {Path.Combine(docsDir, "simulasyon-raporu.md")}");
}
