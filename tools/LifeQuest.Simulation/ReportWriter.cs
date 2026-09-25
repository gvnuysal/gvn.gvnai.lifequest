using System.Text;

namespace LifeQuest.Simulation;

internal static partial class Report
{
    public static partial void Write(string docsDir, ReportData d)
    {
        var images = Path.Combine(docsDir, "images");
        Directory.CreateDirectory(images);

        var a = d.Core.Single(m => m.Scenario.Key == "A");
        var c = d.Core.Single(m => m.Scenario.Key == "C");
        var greedy = d.Core.Single(m => m.Scenario.Key == "D");

        File.WriteAllText(Path.Combine(images, "sim-learning-curve.svg"), Svg.LineChart(
            "Önerilerin isabeti zaman içinde",
            $"Gerçek ilgisi ≥ 0,5 olan öneri payı · {d.Personas.Count * d.Seeds} sentetik kullanıcı · 3 günlük hareketli ortalama",
            [
                ("Tam motor (A)", Rolling(a.DailyPrecision)),
                ("Öğrenme kapalı (C)", Rolling(c.DailyPrecision)),
                ("Yalnızca ilgi (D)", Rolling(greedy.DailyPrecision))
            ],
            "Gün"));

        var tradeoff = new[] { d.Core.Single(m => m.Scenario.Key == "V0"), a, d.Core.Single(m => m.Scenario.Key == "C"), greedy }
            .Concat(d.Ablations).ToList();
        File.WriteAllText(Path.Combine(images, "sim-ablation.svg"), Svg.BarPanels(
            "Her bileşen neyi satın alıyor?",
            "İlk sürüm, güncel tam motor (A) ve ondan tek bir bileşen çıkarılmış sürümler · koyu çubuk: üretimdeki ayar",
            [
                ("North-star / hafta", v => v.ToString("0.00"), 9, tradeoff.Select(m => (Short(m), m.NorthStar, m.Scenario.Key == "A")).ToList()),
                ("Gizli ilgi keşfi", Pct, 1, tradeoff.Select(m => (Short(m), m.HiddenDiscovery, m.Scenario.Key == "A")).ToList()),
                ("Tekrar (düşük iyi)", Pct, 1, tradeoff.Select(m => (Short(m), m.Repetition, m.Scenario.Key == "A")).ToList())
            ]));

        File.WriteAllText(Path.Combine(images, "sim-radius.svg"), Svg.BarPanels(
            "Keşif modu bir tercih: isabet ile keşif arasında",
            "Tüm personalar aynı Discovery Radius ile çalıştırıldığında",
            [
                ("İsabet", Pct, 1, d.Radius.Select(m => (RadiusName(m), m.Precision, m.Scenario.Key == "R-Explore")).ToList()),
                ("Gizli ilgi keşfi", Pct, 1, d.Radius.Select(m => (RadiusName(m), m.HiddenDiscovery, m.Scenario.Key == "R-Explore")).ToList()),
                ("Farklı template (30 gün)", v => v.ToString("0"), 50, d.Radius.Select(m => (RadiusName(m), m.CatalogCoverage, m.Scenario.Key == "R-Explore")).ToList())
            ]));

        bool IsSurpriseProduction(ScenarioMetrics m) => m.Scenario.Weights.NoveltySurpriseMe == d.Tuning.ProductionSurpriseNovelty;
        bool IsChillProduction(ScenarioMetrics m) => m.Scenario.Weights.ExplorationRateChill == d.Tuning.ProductionChillRate;
        string SurpriseLabel(ScenarioMetrics m) => $"Şaşırt · yenilik {m.Scenario.Weights.NoveltySurpriseMe:0.00}";
        string ChillLabel(ScenarioMetrics m) => $"Sakin · keşif %{m.Scenario.Weights.ExplorationRateChill * 100:0}";
        File.WriteAllText(Path.Combine(images, "sim-tuning.svg"), Svg.BarPanels(
            "Mod ayarları: ne kazanıyoruz, ne veriyoruz?",
            "Tüm personalar ilgili moda zorlanarak çalıştırıldı · koyu çubuk: üretimdeki ayar",
            [
                ("North-star / hafta", v => v.ToString("0.00"), 9,
                    [.. d.Tuning.SurpriseSweep.Select(m => (SurpriseLabel(m), m.NorthStar, IsSurpriseProduction(m))),
                     .. d.Tuning.ChillSweep.Select(m => (ChillLabel(m), m.NorthStar, IsChillProduction(m)))]),
                ("Gizli ilgi keşfi", Pct, 1,
                    [.. d.Tuning.SurpriseSweep.Select(m => (SurpriseLabel(m), m.HiddenDiscovery, IsSurpriseProduction(m))),
                     .. d.Tuning.ChillSweep.Select(m => (ChillLabel(m), m.HiddenDiscovery, IsChillProduction(m)))]),
                ("İsabet", Pct, 1,
                    [.. d.Tuning.SurpriseSweep.Select(m => (SurpriseLabel(m), m.Precision, IsSurpriseProduction(m))),
                     .. d.Tuning.ChillSweep.Select(m => (ChillLabel(m), m.Precision, IsChillProduction(m)))])
            ]));

        var findingsPath = Path.GetFullPath(Path.Combine(docsDir, "..", "tools", "LifeQuest.Simulation", "bulgular.md"));
        var findings = File.Exists(findingsPath) ? File.ReadAllText(findingsPath).Trim() : "_Bulgular henüz yazılmadı._";

        var md = new StringBuilder();
        md.AppendLine("# LifeQuest Öneri Motoru — Offline Simülasyon Raporu");
        md.AppendLine();
        md.AppendLine($"> Bu rapor `tools/LifeQuest.Simulation` tarafından üretilir. {d.Catalog.Candidates.Count} template'lik gerçek katalog, " +
                      $"{d.Personas.Count} persona × {d.Seeds} tohum = **{d.Personas.Count * d.Seeds} sentetik kullanıcı**, **{d.Days} gün**. " +
                      "Motor, domain aggregate'leri, ödül hesabı ve öğrenme kuralları üretimdeki kodun kendisidir; yalnızca kullanıcı davranışı modellenir. " +
                      "Sonuçlar deterministiktir: aynı komut aynı sayıları üretir.");
        md.AppendLine();
        md.AppendLine("```bash");
        md.AppendLine("dotnet run --project tools/LifeQuest.Simulation -- --docs docs");
        md.AppendLine("```");
        md.AppendLine();
        md.AppendLine(findings);
        md.AppendLine();

        md.AppendLine("## Sonuçlar");
        md.AppendLine();
        md.AppendLine("![Önerilerin isabeti zaman içinde](images/sim-learning-curve.svg)");
        md.AppendLine();
        md.AppendLine("### Ana senaryolar");
        md.AppendLine();
        Table(md, d.Core);
        md.AppendLine("### Ablasyon: tam motordan tek bileşen çıkarıldığında");
        md.AppendLine();
        md.AppendLine("![Her bileşen neyi satın alıyor?](images/sim-ablation.svg)");
        md.AppendLine();
        Table(md, [a, .. d.Ablations]);
        md.AppendLine("### Cold start: kullanıcı onboarding'de yalnızca tek ilgi beyan ederse");
        md.AppendLine();
        Table(md, d.Sparse);
        md.AppendLine("### Discovery Radius");
        md.AppendLine();
        md.AppendLine("![Keşif modu bir tercih](images/sim-radius.svg)");
        md.AppendLine();
        Table(md, d.Radius);
        md.AppendLine("### Mod ayarları: Şaşırt Beni yeniliği ve Sakin keşif oranı");
        md.AppendLine();
        md.AppendLine("![Mod ayarı taraması](images/sim-tuning.svg)");
        md.AppendLine();
        md.AppendLine($"Herkes ilgili moda zorlanarak çalıştırıldı. Üretim değerleri: Şaşırt Beni yeniliği **{d.Tuning.ProductionSurpriseNovelty:0.00}**, Sakin keşif oranı **{d.Tuning.ProductionChillRate:0.0}**.");
        md.AppendLine();
        Table(md, [.. d.Tuning.SurpriseSweep, .. d.Tuning.ChillSweep]);
        md.AppendLine("Persona bazında önceki mod ayarları (A0: yenilik 0,35, Sakin'de keşif yok) ile güncel ayarlar (A):");
        md.AppendLine();
        md.AppendLine("| Persona | Keşif modu | İsabet A0 → A | North-star A0 → A | Gizli ilgi keşfi A0 → A |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (var ((p, before), (_, after)) in d.Tuning.PersonasBefore.Zip(d.Personas))
            md.AppendLine($"| {p.Name} | {p.Radius} | {Pct(before.Precision)} → {Pct(after.Precision)} | {before.NorthStar:0.00} → {after.NorthStar:0.00} | {Pct(before.HiddenDiscovery)} → {Pct(after.HiddenDiscovery)} |");
        md.AppendLine();
        md.AppendLine("### Erişilebilirlik: hareket kısıtı olan persona");
        md.AppendLine();
        md.AppendLine("| Senaryo | Kapasitesini aşan öneri | İsabet | North-star / hafta |");
        md.AppendLine("|---|---|---|---|");
        md.AppendLine($"| Efor sınırı beyan edildi | {Pct(d.MobilityWith.AboveAbilityShare)} | {Pct(d.MobilityWith.Precision)} | {d.MobilityWith.NorthStar:0.00} |");
        md.AppendLine($"| Efor sınırı beyan edilmedi | {Pct(d.MobilityWithout.AboveAbilityShare)} | {Pct(d.MobilityWithout.Precision)} | {d.MobilityWithout.NorthStar:0.00} |");
        md.AppendLine();
        md.AppendLine("### Persona kırılımı (senaryo A)");
        md.AppendLine();
        md.AppendLine("| Persona | Keşif modu | İsabet | İlk 5 gün | Son 5 gün | North-star / hafta | Tamamlanan kategori | Gizli ilgi keşfi |");
        md.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var (p, m) in d.Personas)
            md.AppendLine($"| {p.Name} | {p.Radius} | {Pct(m.Precision)} | {Pct(m.EarlyPrecision)} | {Pct(m.LatePrecision)} | {m.NorthStar:0.00} | {m.CategoryCoverage:0.0} | {Pct(m.HiddenDiscovery)} |");
        md.AppendLine();

        md.AppendLine("## Yöntem");
        md.AppendLine();
        md.AppendLine("**Personalar.** Her personanın gizli bir gerçek ilgi haritası vardır (listede olmayan ilgiler 0,15). Onboarding'de bunun yalnızca bir kısmını beyan eder; " +
                      "\"gizli\" ilgiler kullanıcının sevdiği ama söylemediği alanlardır. Sistem bunları keşfederse (öğrenilmiş ağırlık ≥ 0,4 ya da o etiketle bir quest tamamlanırsa) \"gizli ilgi keşfi\" sayılır.");
        md.AppendLine();
        md.AppendLine("| Persona | Beyan edilen | Gizli | Bütçe | Haftalık süre | Şehir | Fiziksel kapasite |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var (p, _) in d.Personas)
            md.AppendLine($"| {p.Name} | {string.Join(", ", p.Declared)} | {string.Join(", ", p.Hidden)} | {p.Budget} | {p.WeeklyMinutes} dk | {(p.HasCity ? "var" : "yok")} | {p.Ability} |");
        md.AppendLine();
        md.AppendLine("**Davranış modeli.** Her gün motor 3 öneri üretir (yerel saat 18:30). Kabul olasılığı `0,85 × ilgi^1,5`; " +
                      "bütçe konforunu aşarsa ×0,2, oturum süresini aşarsa ×0,35, fiziksel kapasiteyi aşarsa 0. Aynı anda en fazla 5 aktif görev. " +
                      "Kabul edilen görev `0,55 + 0,4 × ilgi` olasılıkla tamamlanır (günlük aynı gün, haftalık 1-4 gün, macera 3-10 gün). " +
                      "Tamamlananların %70'i puanlanır: `1 + 4 × (ilgi ± gürültü)`; 5 puanın bir kısmı \"daha fazla\", 1-2 puanın bir kısmı \"daha az\" tercihi taşır. " +
                      "Kabul edilmeyen önerilerin %60'ı sebep belirtilerek geçilir (ilgi < 0,35 → ilgimi çekmedi, sonra pahalı / zamanım yok / bugün değil). " +
                      "Öğrenme adımları API handler'larıyla aynı `InterestLearning` sabitlerini kullanır.");
        md.AppendLine();
        md.AppendLine("**Metrikler.**");
        md.AppendLine();
        md.AppendLine("- **İsabet:** gerçek ilgisi ≥ 0,5 olan öneri payı (± kullanıcılar arası standart hata).");
        md.AppendLine("- **North-star:** haftalık anlamlı deneyim; tamamlanmış ve puansız ya da ≥ 4 puanlı quest sayısı.");
        md.AppendLine("- **Tekrar:** son 7 günde aynı kullanıcıya zaten gösterilmiş template'in payı.");
        md.AppendLine("- **Çeşitlilik:** günlük 3'lü listedeki farklı kategori sayısı.");
        md.AppendLine("- **Katalog kapsamı:** 30 günde gösterilen farklı template sayısı.");
        md.AppendLine("- **Gün 1:** ilk günün ilk önerisi hem kısa hem ilgili mi (\"ilk 5 dakikada uygun quest\" başarı ölçütü).");
        md.AppendLine();
        md.AppendLine("## Sınırlar");
        md.AppendLine();
        md.AppendLine("- Davranış modeli bir varsayımdır; mutlak değerler değil, **senaryolar arası farklar** yorumlanmalıdır (ortak rastgele sayılar kullanıldığı için bu farklar gürültüye karşı dayanıklıdır).");
        md.AppendLine("- \"Gerçek ilgi\" statiktir; gerçek kullanıcıların zevki deneyimle değişir. Ayrıca hava, mekân açıklığı ve sosyal etki modellenmedi.");
        md.AppendLine("- Gerçek kullanıcı verisi geldiğinde aynı metrikler `/api/v1/admin/metrics` ve saklanan skor dökümleriyle doğrulanmalı, ağırlıklar A/B testiyle ayarlanmalıdır.");

        File.WriteAllText(Path.Combine(docsDir, "simulasyon-raporu.md"), md.ToString());
    }

    private static void Table(StringBuilder md, IEnumerable<ScenarioMetrics> metrics)
    {
        md.AppendLine("| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var m in metrics)
            md.AppendLine($"| **{m.Scenario.Key}** {m.Scenario.Name} | {Pct(m.Precision)} ±{m.PrecisionSe * 100:0} | {Pct(m.EarlyPrecision)} → {Pct(m.LatePrecision)} | {Pct(m.Acceptance)} | {m.NorthStar:0.00} ±{m.NorthStarSe:0.00} | {m.AverageRating:0.0} | {m.CategoryCoverage:0.0} | {m.CatalogCoverage:0} | {Pct(m.Repetition)} | {Pct(m.HiddenDiscovery)} ±{m.HiddenDiscoverySe * 100:0} | {Pct(m.FirstDayShortAndRelevant)} |");
        md.AppendLine();
    }

    private static double[] Rolling(double[] values)
        => values.Select((_, i) => values[Math.Max(0, i - 2)..(i + 1)].Average()).ToArray();

    private static string Short(ScenarioMetrics m) => m.Scenario.Key switch
    {
        "A" => "A · tam motor",
        "V0" => "V0 · ilk sürüm",
        "C" => "C · öğrenme yok",
        "D" => "D · yalnızca ilgi",
        "X-Rep" => "− tekrar cezası",
        "X-Nov" => "− yenilik",
        "X-Div" => "− çeşitlilik",
        "X-Exp" => "− keşif slotu",
        "X-Ign3" => "kısa tekrar penceresi",
        _ => m.Scenario.Name
    };

    private static string RadiusName(ScenarioMetrics m) => m.Scenario.Key switch
    {
        "R-Chill" => "Sakin",
        "R-Explore" => "Dengeli",
        _ => "Şaşırt Beni"
    };
}
