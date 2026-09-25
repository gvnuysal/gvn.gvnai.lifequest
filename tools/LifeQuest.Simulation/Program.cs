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
        GuidedExploration = false, ExplorationRateExplore = 1.0, ExplorationRateChill = 0,
        IgnoredOfferWindowDays = 3, NoveltySurpriseMe = 0.35, LovedRepeatNovelty = 0.2
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

// Mod ayarı taraması: herkes ilgili moda zorlanır; üretim değeri raporda vurgulanır.
Scenario[] surpriseSweep = [.. new[] { 0.35, 0.30, 0.25, 0.20 }
    .Select(n => new Scenario($"SN{n:0.00}", $"Şaşırt Beni, yenilik {n:0.00}", production with { NoveltySurpriseMe = n },
        RadiusOverride: DiscoveryRadius.SurpriseMe))];
Scenario[] chillSweep = [.. new[] { 0.0, 0.1, 0.2, 0.3 }
    .Select(rate => new Scenario($"CR{rate:0.0}", $"Sakin, keşif oranı {rate:0.0}", production with { ExplorationRateChill = rate },
        RadiusOverride: DiscoveryRadius.Chill))];
// Sevdiğini tekrarla: 5 puan verilen template'in cooldown sonrası yenilik skoru (0.2 = özellik kapalı).
Scenario[] lovedSweep = [.. new[] { 0.2, 0.4, 0.6, 0.8 }
    .Select(n => new Scenario($"LR{n:0.0}", $"Sevdiğini tekrarla, yenilik {n:0.0}", production with { LovedRepeatNovelty = n }))];

var previousModes = new Scenario("A0", "Tam motor, önceki mod ayarları",
    production with { NoveltySurpriseMe = 0.35, ExplorationRateChill = 0 });

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
var surpriseMetrics = surpriseSweep.Select(s => ScenarioMetrics.From(s, RunAll(s), Days)).ToList();
var chillMetrics = chillSweep.Select(s => ScenarioMetrics.From(s, RunAll(s), Days)).ToList();
var lovedMetrics = lovedSweep.Select(s => ScenarioMetrics.From(s, RunAll(s), Days)).ToList();
var previousRuns = RunAll(previousModes);

var full = core.Single(s => s.Key == "A");
var mobility = Persona.All.Where(p => p.Key == "mobility").ToList();
var mobilityWith = ScenarioMetrics.From(full, RunAll(full, mobility), Days);
var mobilityWithout = ScenarioMetrics.From(effortOff, RunAll(effortOff, mobility), Days);

var perPersona = Persona.All
    .Select(p => (Persona: p, Metrics: ScenarioMetrics.From(full, coreRuns[full].Where(r => r.Persona == p).ToList(), Days)))
    .ToList();
var perPersonaBefore = Persona.All
    .Select(p => (Persona: p, Metrics: ScenarioMetrics.From(previousModes, previousRuns.Where(r => r.Persona == p).ToList(), Days)))
    .ToList();

Console.WriteLine($"Katalog: {catalog.Candidates.Count} template, {catalog.StarterCards.Count} başlangıç kartı · {Persona.All.Count} persona × {SeedsPerPersona} tohum × {Days} gün");
Console.WriteLine();
Report.PrintTable(coreMetrics.Concat(ablationMetrics).Concat(sparseMetrics).Concat(radiusMetrics).Append(mobilityWith with { Scenario = mobilityWith.Scenario with { Name = "Hareket kısıtlı · efor beyanlı" } })
    .Append(mobilityWithout with { Scenario = mobilityWithout.Scenario with { Name = "Hareket kısıtlı · efor beyansız" } }));
Console.WriteLine();
Report.PrintPersonas(perPersona);
Console.WriteLine();
Report.PrintTable(surpriseMetrics.Concat(chillMetrics).Concat(lovedMetrics));
Console.WriteLine();
Report.PrintPersonas(perPersonaBefore, "Persona (A0)");

if (docsDir is not null)
{
    Report.Write(docsDir, new ReportData(catalog, coreMetrics, ablationMetrics, sparseMetrics, radiusMetrics,
        mobilityWith, mobilityWithout, perPersona,
        new TuningData(surpriseMetrics, chillMetrics, production.NoveltySurpriseMe, production.ExplorationRateChill, perPersonaBefore,
            lovedMetrics, production.LovedRepeatNovelty),
        Days, SeedsPerPersona));
    Console.WriteLine($"\nRapor yazıldı: {Path.Combine(docsDir, "simulasyon-raporu.md")}");
}
