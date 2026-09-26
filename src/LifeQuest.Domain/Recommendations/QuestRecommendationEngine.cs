using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.RealWorld;

namespace LifeQuest.Domain.Recommendations;

/// <summary>
/// Saf (I/O'suz, deterministik) öneri motoru. Akış:
/// <list type="number">
/// <item>Eligibility: güvenlik, cooldown, açık quest, bütçe, süre, şehir ve "ilgimi çekmedi" filtreleri.</item>
/// <item>Skor: Interest + Novelty + Context + GoalFit + Diversity + FeedbackFit - Repetition - Friction - Risk.</item>
/// <item>Seçim: diversity her seçimden sonra yeniden hesaplanır (MMR benzeri); ilk slot mümkünse kısa bir
/// quest, keşif modlarında bir slot kontrollü keşif (seeded, tekrarlanabilir).</item>
/// <item>Açıklama: en etkili bileşenlerden "neden bunu önerdim?" metni.</item>
/// </list>
/// </summary>
public sealed class QuestRecommendationEngine(RecommendationWeights weights)
{
    private const int ShortQuestMaxMinutes = 30;

    public RecommendationResult Recommend(
        IReadOnlyList<QuestCandidate> candidates,
        RecommendationProfile profile,
        RecommendationHistory history,
        TasteGraph graph,
        RecommendationContext context)
    {
        var filtered = new Dictionary<string, int>();
        var eligible = candidates
            .Where(c => IsEligible(c, profile, history, context, filtered))
            .Select(c => ScoreCandidate(c, profile, history, graph, context))
            .ToList();

        var selected = Select(eligible, profile, context);

        var dominant = DominantRecentCategory(history, context.UtcNow);
        var items = selected
            .Select(s =>
            {
                var reasons = BuildReasons(s, profile, graph, context, dominant);
                return new RecommendedQuest(
                    s.Scored.Candidate, s.Breakdown, s.IsExploration, reasons,
                    ExplanationBuilder.Build(reasons, s.Scored.Candidate.Category));
            })
            .ToList();

        return new RecommendationResult(items, candidates.Count, eligible.Count, filtered);
    }

    // ── 1. Eligibility ───────────────────────────────────────────────────────

    private bool IsEligible(
        QuestCandidate c, RecommendationProfile profile, RecommendationHistory history,
        RecommendationContext context, Dictionary<string, int> filtered)
    {
        var reason = EligibilityFailure(c, profile, history, context);
        if (reason is null)
            return true;

        filtered[reason] = filtered.GetValueOrDefault(reason) + 1;
        return false;
    }

    private string? EligibilityFailure(
        QuestCandidate c, RecommendationProfile profile, RecommendationHistory history, RecommendationContext context)
    {
        if (history.ActiveTemplateIds.Contains(c.TemplateId))
            return "already_open";

        if (history.CompletedTemplates.TryGetValue(c.TemplateId, out var lastCompleted) &&
            lastCompleted > context.UtcNow.AddDays(-c.CooldownDays))
            return "cooldown";

        var maxCost = context.MaxCost ?? profile.Budget;
        if (c.Cost > maxCost)
            return "over_budget";

        if (c.RequiresCity && !profile.HasCity)
            return "requires_city";

        if (context.AvailableMinutes is { } available && c.MinMinutes > available)
            return "too_long";

        // Erişilebilirlik: kullanıcının belirttiği efor sınırı aşılmaz.
        if (c.Effort > profile.MaxEffort)
            return "effort_limit";

        // Güvenlik: "şimdi yapılacak" bağlamda (günlük kısa görev veya "şu kadar vaktim var") gece açık hava önerilmez.
        var isNight = DayParts.FromHour(context.LocalNow.Hour) == DayPart.Night;
        if (isNight && c.IsOutdoor && (c.Type == QuestType.Daily || context.AvailableMinutes is not null))
            return "outdoor_at_night";

        // Aynı kural hava için: yağmur/fırtına/aşırı sıcaklıkta "şimdi" yapılacak açık hava görevi önerilmez.
        if (context.Weather == OutdoorWeather.Poor && c.IsOutdoor && (c.Type == QuestType.Daily || context.AvailableMinutes is not null))
            return "bad_weather";

        var rejected = history.Items.Any(i => i.TemplateId == c.TemplateId && (
            (i.SkipReason == SkipReason.NotInterested &&
             i.LastActivityAt > context.UtcNow.AddDays(-weights.NotInterestedBlockDays)) ||
            (i.Preference == FeedbackPreference.LessLikeThis &&
             i.LastActivityAt > context.UtcNow.AddDays(-weights.LessLikeThisBlockDays))));

        return rejected ? "rejected_by_user" : null;
    }

    // ── 2. Skorlama ──────────────────────────────────────────────────────────

    private ScoredCandidate ScoreCandidate(
        QuestCandidate c, RecommendationProfile profile, RecommendationHistory history,
        TasteGraph graph, RecommendationContext context)
    {
        var interest = InterestScore(c, profile, graph);

        return new ScoredCandidate(
            c,
            interest,
            Novelty(c, history),
            Context(c, context) + RealWorldBonus(c, context),
            GoalFit(c, profile),
            FeedbackFit(c, history),
            Repetition(c, history, context),
            Friction(c, profile, history, context),
            c.RiskScore,
            history.LovedTemplates.Contains(c.TemplateId) && history.CompletedTemplates.ContainsKey(c.TemplateId));
    }

    internal InterestMatch InterestScore(QuestCandidate c, RecommendationProfile profile, TasteGraph graph)
    {
        var best = new InterestMatch(weights.BaselineInterest, null, null);

        foreach (var tag in c.InterestIds)
        {
            if (profile.InterestWeights.TryGetValue(tag, out var direct) && direct > best.Score)
                best = new InterestMatch(direct, tag, null);

            foreach (var (neighbor, strength) in graph.Neighbors(tag))
            {
                if (!profile.InterestWeights.TryGetValue(neighbor, out var userWeight))
                    continue;

                var adjacent = userWeight * strength * weights.AdjacencyFactor;
                if (adjacent > best.Score)
                    best = new InterestMatch(adjacent, tag, neighbor);
            }
        }

        return best;
    }

    /// <summary>
    /// Yeni kategori 1.0, bilinen kategoride yeni template 0.6, daha önce yapılmış template 0.2. Çok sevilen
    /// (5 puan / "daha fazla") bir deneyim cooldown'dan sonra "tekrar yaşanmaya değer" sayılır ve daha az cezalanır.
    /// </summary>
    private double Novelty(QuestCandidate c, RecommendationHistory history)
    {
        if (!history.CompletedCategories.Contains(c.Category))
            return 1.0;

        if (!history.CompletedTemplates.ContainsKey(c.TemplateId))
            return 0.6;

        return history.LovedTemplates.Contains(c.TemplateId) ? Math.Max(0.2, weights.LovedRepeatNovelty) : 0.2;
    }

    private static double Context(QuestCandidate c, RecommendationContext context)
    {
        var dayFit = c.Type != QuestType.Daily || (c.DayParts & DayParts.FromHour(context.LocalNow.Hour)) != 0
            ? 1.0
            : 0.3;

        var timeFit = context.AvailableMinutes is not { } available || c.MaxMinutes <= available ? 1.0 : 0.6;

        var isWeekend = context.LocalNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var weekendFit = c.Type != QuestType.Daily && !isWeekend ? 0.8 : 1.0;

        return dayFit * 0.5 + timeFit * 0.3 + weekendFit * 0.2;
    }

    /// <summary>Gerçek dünya bağlamı: şehirde etkinlik ve güzel hava. Veri yoksa 0 (simülasyon ve şehirsiz kullanıcı).</summary>
    private double RealWorldBonus(QuestCandidate c, RecommendationContext context)
    {
        var bonus = context.HasLocalEvent(c.TemplateId) ? weights.LocalEventBoost : 0;
        if (IsGoodWeatherOutdoor(c, context))
            bonus += weights.GoodWeatherOutdoorBoost;
        return bonus;
    }

    private static bool IsGoodWeatherOutdoor(QuestCandidate c, RecommendationContext context)
        => c.IsOutdoor && context.Weather == OutdoorWeather.Good && DayParts.FromHour(context.LocalNow.Hour) != DayPart.Night;

    private static double GoalFit(QuestCandidate c, RecommendationProfile profile)
    {
        if (profile.Goals.Count == 0) return 0.5;
        if (profile.Goals.Contains(c.Category)) return 1.0;
        return c.SecondaryCategory is { } secondary && profile.Goals.Contains(secondary) ? 0.5 : 0.0;
    }

    private static double FeedbackFit(QuestCandidate c, RecommendationHistory history)
    {
        var ratings = history.Items
            .Where(i => i.Category == c.Category && i.Rating is not null)
            .Select(i => (i.Rating!.Value - 1) / 4.0)
            .ToList();

        var score = ratings.Count > 0 ? ratings.Average() : 0.5;

        var tags = c.InterestIds.ToHashSet();
        foreach (var item in history.Items.Where(i => i.Preference is not null && i.InterestIds.Any(tags.Contains)))
            score += item.Preference == FeedbackPreference.MoreLikeThis ? 0.15 : -0.15;

        return Math.Clamp(score, 0, 1);
    }

    private double Repetition(QuestCandidate c, RecommendationHistory history, RecommendationContext context)
    {
        var recent = history
            .Since(context.UtcNow.AddDays(-weights.RecentWindowDays))
            .Where(i => i.Status is QuestStatus.Completed or QuestStatus.Accepted)
            .ToList();

        var penalty = recent.Count == 0
            ? 0
            : recent.Count(i => i.Category == c.Category) / (double)Math.Max(3, recent.Count);

        // "Gösterildi ama seçilmedi" örtük bir olumsuz sinyaldir: aynı template'i kısa aralıkla tekrar göstermek
        // hem öneri alanını israf eder hem de "hep aynı şeyler" hissi yaratır.
        var sameTemplateRecently = history.Since(context.UtcNow.AddDays(-weights.IgnoredOfferWindowDays))
            .Where(i => i.TemplateId == c.TemplateId).ToList();
        if (sameTemplateRecently.Any(i => i.Status == QuestStatus.Skipped)) penalty += 0.5;
        penalty += Math.Min(0.6, weights.IgnoredOfferPenalty * sameTemplateRecently.Count(i => i.Status == QuestStatus.Expired));

        return Math.Clamp(penalty, 0, 1);
    }

    private static double Friction(
        QuestCandidate c, RecommendationProfile profile, RecommendationHistory history, RecommendationContext context)
    {
        var friction = 0.0;

        var maxCost = context.MaxCost ?? profile.Budget;
        if (c.Cost == maxCost && maxCost > CostBand.Free)
            friction += 0.3;

        var weeklyShare = c.MaxMinutes / (double)Math.Max(30, profile.WeeklyAvailableMinutes);
        friction += weeklyShare > 0.5 ? 0.4 : weeklyShare > 0.25 ? 0.2 : 0;

        var recentSkips = history.Since(context.UtcNow.AddDays(-14)).Where(i => i.SkipReason is not null).ToList();
        if (c.Cost >= CostBand.Medium && recentSkips.Any(s => s.SkipReason == SkipReason.TooExpensive)) friction += 0.3;
        if (c.MaxMinutes >= 60 && recentSkips.Any(s => s.SkipReason == SkipReason.NoTime)) friction += 0.3;
        if (c.RequiresCity && recentSkips.Any(s => s.SkipReason == SkipReason.TooFar)) friction += 0.2;

        return Math.Clamp(friction, 0, 1);
    }

    private static double Diversity(ScoredCandidate candidate, IReadOnlyCollection<ScoredCandidate> selected)
    {
        var sameCategory = selected.Count(s => s.Candidate.Category == candidate.Candidate.Category);
        var tags = candidate.Candidate.InterestIds.ToHashSet();
        var sharedTags = selected.Count(s => s.Candidate.InterestIds.Any(tags.Contains));
        return Math.Clamp(1 - 0.5 * sameCategory - 0.2 * sharedTags, 0, 1);
    }

    private ScoreBreakdown Total(ScoredCandidate s, double diversity, RecommendationProfile profile)
    {
        var total =
            weights.InterestWeightFor(profile.Radius) * s.Interest.Score +
            weights.NoveltyWeightFor(profile.Radius) * s.Novelty +
            weights.Context * s.Context +
            weights.GoalFit * s.GoalFit +
            weights.Diversity * diversity +
            weights.FeedbackFit * s.FeedbackFit -
            weights.Repetition * s.Repetition -
            weights.Friction * s.Friction -
            weights.Risk * s.Risk;

        return new ScoreBreakdown(
            R(s.Interest.Score), R(s.Novelty), R(s.Context), R(s.GoalFit), R(diversity),
            R(s.FeedbackFit), R(s.Repetition), R(s.Friction), R(s.Risk), R(total));
    }

    private static double R(double value) => Math.Round(value, 4);

    // ── 3. Seçim ─────────────────────────────────────────────────────────────

    private List<Selection> Select(List<ScoredCandidate> eligible, RecommendationProfile profile, RecommendationContext context)
    {
        var remaining = eligible.ToList();
        var selected = new List<Selection>();
        var random = new Random(context.Seed);
        var explorationSlots = RecommendationWeights.ExplorationSlotsFor(context.Count);
        if (random.NextDouble() >= weights.ExplorationRateFor(profile.Radius))
            explorationSlots = 0;

        while (selected.Count < context.Count && remaining.Count > 0)
        {
            var picked = selected.Select(s => s.Scored).ToList();
            var ranked = remaining
                .Select(c => (Candidate: c, Breakdown: Total(c, Diversity(c, picked), profile)))
                .OrderByDescending(x => x.Breakdown.Total)
                .ThenBy(x => x.Candidate.Candidate.Code, StringComparer.Ordinal)
                .ToList();

            var isExplorationSlot = explorationSlots > 0 && selected.Count == context.Count - explorationSlots;
            (ScoredCandidate Candidate, ScoreBreakdown Breakdown) choice;
            var isExploration = false;

            if (selected.Count == 0 && context.RequireShortQuest && ranked.Any(x => IsShort(x.Candidate.Candidate)))
            {
                choice = ranked.First(x => IsShort(x.Candidate.Candidate));
            }
            else if (isExplorationSlot && ExplorationPool(ranked, adjacentOnly: profile.Radius == DiscoveryRadius.Chill) is { Count: > 0 } pool)
            {
                choice = pool[random.Next(pool.Count)];
                isExploration = true;
            }
            else
            {
                choice = ranked[0];
            }

            selected.Add(new Selection(choice.Candidate, choice.Breakdown, isExploration));
            remaining.Remove(choice.Candidate);
        }

        return selected;
    }

    /// <summary>
    /// Kontrollü keşif havuzu. Önce Taste Graph ile kullanıcının sevdiği bir ilgiye komşu olan adaylar
    /// denenir (güdümlü keşif: "kahve seviyorsan mimariye bak"); böyle aday yoksa ilgi skoru düşük ama yeni
    /// alanlara düşülür. Offline simülasyonda rastgele keşif, kabul oranı düşük olduğu için north-star'ı
    /// düşürüp gizli ilgi keşfine çok az katkı veriyordu (docs/simulasyon-raporu.md).
    /// Sakin modda (<paramref name="adjacentOnly"/>) yalnızca komşu adaylar kullanılır; rastgele alana düşülmez.
    /// </summary>
    private List<(ScoredCandidate Candidate, ScoreBreakdown Breakdown)> ExplorationPool(
        List<(ScoredCandidate Candidate, ScoreBreakdown Breakdown)> ranked, bool adjacentOnly)
    {
        var eligible = ranked
            .Where(x => x.Candidate.Novelty >= weights.ExplorationMinNovelty &&
                        x.Candidate.Interest.Score <= weights.ExplorationMaxInterest &&
                        x.Candidate.Risk <= 0.2)
            .ToList();

        var adjacent = weights.GuidedExploration
            ? eligible.Where(x => x.Candidate.Interest.ViaInterestId is not null).Take(3).ToList()
            : [];
        if (adjacent.Count > 0 || adjacentOnly)
            return adjacent;
        return eligible.Take(3).ToList();
    }

    private static bool IsShort(QuestCandidate c) => c.Type == QuestType.Daily || c.MaxMinutes <= ShortQuestMaxMinutes;

    // ── 4. Açıklama ──────────────────────────────────────────────────────────

    private static LifeCategory? DominantRecentCategory(RecommendationHistory history, DateTime utcNow)
    {
        var recent = history.Since(utcNow.AddDays(-7))
            .Where(i => i.Status == QuestStatus.Completed)
            .ToList();

        if (recent.Count < 2)
            return null;

        var top = recent.GroupBy(i => i.Category).OrderByDescending(g => g.Count()).First();
        return top.Count() * 2 >= recent.Count ? top.Key : null;
    }

    private static List<RecommendationReason> BuildReasons(
        Selection s, RecommendationProfile profile, TasteGraph graph, RecommendationContext context, LifeCategory? dominant)
    {
        var c = s.Scored.Candidate;
        var reasons = new List<RecommendationReason>();

        if (dominant is { } d && d != c.Category)
            reasons.Add(new(ReasonCode.Diversification, $"son aktivitelerinde {d.DisplayName()} ağırlığı olduğu"));

        if (s.IsExploration)
            reasons.Add(new(ReasonCode.ExplorationPick, $"{profile.Radius.DisplayName()} modunu seçtiğin"));

        if (s.Scored.LovedBefore)
            reasons.Add(new(ReasonCode.LovedBefore, "daha önce çok sevdiğin bir deneyim olduğu"));

        var interest = s.Scored.Interest;
        if (interest.ViaInterestId is { } via && interest.MatchedInterestId is { } target)
            reasons.Add(new(ReasonCode.AdjacentInterest,
                $"{graph.NameOf(via)} ilgin {graph.NameOf(target)} alanına kapı açtığı"));

        if (s.Scored.Novelty >= 1.0)
            reasons.Add(new(ReasonCode.NewCategory, $"{c.Category.DisplayName()} alanında henüz quest tamamlamadığın"));

        if (interest.ViaInterestId is null && interest.MatchedInterestId is { } matched && interest.Score >= 0.6)
            reasons.Add(new(ReasonCode.InterestMatch, $"{graph.NameOf(matched)} ilginle örtüştüğü"));

        if (s.Scored.GoalFit >= 1.0)
            reasons.Add(new(ReasonCode.GoalFit, $"{c.Category.DisplayName()} hedeflerin arasında olduğu"));

        if (context.AvailableMinutes is { } available && c.MaxMinutes <= available)
            reasons.Add(new(ReasonCode.FitsAvailableTime, $"ayırdığın {available} dakikaya sığdığı"));

        if (context.HasLocalEvent(c.TemplateId))
            reasons.Insert(0, new(ReasonCode.LocalEvent, "şehrinde bu hafta ilgili bir etkinlik olduğu"));

        if (IsGoodWeatherOutdoor(c, context))
            reasons.Add(new(ReasonCode.GoodWeather, "hava açık hava için çok uygun olduğu"));

        if (c.Cost == CostBand.Free)
            reasons.Add(new(ReasonCode.Free, "ücretsiz olduğu"));

        return reasons;
    }

    // ── İç modeller ──────────────────────────────────────────────────────────

    /// <param name="MatchedInterestId">Quest'in eşleşen etiketi.</param>
    /// <param name="ViaInterestId">Eşleşme Taste Graph komşuluğundan geldiyse kullanıcının ilgisi.</param>
    internal sealed record InterestMatch(double Score, Guid? MatchedInterestId, Guid? ViaInterestId);

    private sealed record ScoredCandidate(
        QuestCandidate Candidate,
        InterestMatch Interest,
        double Novelty,
        double Context,
        double GoalFit,
        double FeedbackFit,
        double Repetition,
        double Friction,
        double Risk,
        bool LovedBefore);

    private sealed record Selection(ScoredCandidate Scored, ScoreBreakdown Breakdown, bool IsExploration);
}

/// <summary>
/// Gerekçe cümlecikleri "… olduğu ve … seçtiğin için bu kez bir Kültür quest'i önerdik." kalıbında birleştirilir.
/// </summary>
internal static class ExplanationBuilder
{
    private static readonly System.Globalization.CultureInfo Turkish = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    public static string Build(IReadOnlyList<RecommendationReason> reasons, LifeCategory category)
    {
        if (reasons.Count == 0)
            return "Profiline ve bugünkü bağlamına en uygun seçeneklerden biri.";

        var clauses = string.Join(" ve ", reasons.Take(2).Select(r => r.Text));
        var tail = reasons.Take(2).Any(r => r.Code == ReasonCode.Diversification)
            ? $" için bu kez bir {category.DisplayName()} quest'i önerdik."
            : " için önerdik.";

        return char.ToUpper(clauses[0], Turkish) + clauses[1..] + tail;
    }
}
