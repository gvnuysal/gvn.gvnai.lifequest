using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Simulation;

internal sealed record Scenario(
    string Key,
    string Name,
    RecommendationWeights Weights,
    bool Learning = true,
    bool StarterCards = true,
    DiscoveryRadius? RadiusOverride = null,
    bool DeclareEffortLimit = true,
    bool SparseDeclaration = false);

internal sealed record OfferRecord(
    int Day, string Code, LifeCategory Category, double Affinity, bool IsShort, bool IsExploration,
    bool AboveAbility, bool Accepted, int Slot);

internal sealed record CompletionRecord(int Day, LifeCategory Category, int? Rating, bool NewCategory);

internal sealed class UserRun
{
    public required Persona Persona { get; init; }
    public List<OfferRecord> Offers { get; } = [];
    public List<CompletionRecord> Completions { get; } = [];
    public int ShortfallDays { get; set; }
    public int HiddenInterestsDiscovered { get; set; }
    public int LifeLevel { get; set; }
}

/// <summary>
/// Tek bir sentetik kullanıcının N günlük yolculuğu. Üretimdeki motor, aggregate'ler, ödül hesabı ve öğrenme
/// kuralları (<see cref="InterestLearning"/>) aynen kullanılır; yalnızca kullanıcı davranışı modellenir.
/// </summary>
internal sealed class Simulator(SimulationCatalog catalog)
{
    private static readonly TimeZoneInfo Istanbul = TimeZones.Resolve("Europe/Istanbul");
    private static readonly DateTime StartLocal = new(2026, 10, 5, 0, 0, 0); // Pazartesi

    public UserRun Run(Persona persona, Scenario scenario, int seed, int days)
    {
        // Ortak rastgele sayılar: aynı persona + tohum her senaryoda aynı davranış dizisini üretir (adil karşılaştırma).
        var rng = new Random(BitConverter.ToInt32(SimulationCatalog.DeterministicGuid($"rng:{persona.Key}:{seed}").ToByteArray(), 0));
        var userId = SimulationCatalog.DeterministicGuid($"user:{persona.Key}:{seed}");
        var engine = new QuestRecommendationEngine(scenario.Weights);
        var run = new UserRun { Persona = persona };

        var start = ToUtc(StartLocal.AddHours(9));
        var profile = UserProfile.CreateFor(userId);
        profile.CompleteOnboarding(
            new ProfilePreferences(
                scenario.RadiusOverride ?? persona.Radius, persona.Budget, persona.WeeklyMinutes, persona.Goals,
                persona.HasCity ? "İstanbul" : null, ClearCity: !persona.HasCity, "Europe/Istanbul",
                scenario.DeclareEffortLimit ? persona.Ability : PhysicalEffort.Vigorous),
            persona.Declared.Take(scenario.SparseDeclaration ? 1 : int.MaxValue)
                .Select((code, i) => new InterestSelection(catalog.InterestIds[code], i == 0 ? 0.9 : 0.6)).ToList(),
            start);

        if (scenario.StarterCards && scenario.Learning)
            ReactToStarterCards(persona, profile, start);

        var progress = PlayerProgress.CreateFor(userId);
        var quests = new List<UserQuest>();
        var scheduled = new List<(UserQuest Quest, int Day, double Affinity)>();

        for (var day = 0; day < days; day++)
        {
            var localNow = StartLocal.AddDays(day).AddHours(18).AddMinutes(30);
            var utcNow = ToUtc(localNow);

            foreach (var quest in quests) quest.TryExpire(utcNow);

            foreach (var item in scheduled.Where(s => s.Day <= day).ToList())
            {
                scheduled.Remove(item);
                CompleteQuest(item.Quest, item.Affinity, persona, profile, progress, run, scenario, rng, day, utcNow.AddHours(-1));
            }

            var history = BuildHistory(quests, progress, utcNow);
            var recommendationProfile = new RecommendationProfile(
                profile.DiscoveryRadius, profile.Budget, profile.WeeklyAvailableMinutes, profile.Goals.ToHashSet(),
                profile.InterestWeights(), profile.City is not null, profile.MaxPhysicalEffort);

            var date = DateOnly.FromDateTime(localNow);
            var result = engine.Recommend(catalog.Candidates, recommendationProfile, history, catalog.Graph,
                new RecommendationContext(localNow, utcNow, 3, BitConverter.ToInt32(userId.ToByteArray(), 0) ^ date.DayNumber));

            if (result.Items.Count < 3) run.ShortfallDays++;

            var expires = TimeZones.EndOfLocalDayUtc(date, Istanbul);
            for (var slot = 0; slot < result.Items.Count; slot++)
            {
                var item = result.Items[slot];
                var c = item.Candidate;
                var reward = RewardCalculator.Calculate(c.Type, c.Difficulty, c.SecondaryCategory is not null,
                    RewardCalculator.NoveltyMultiplier(history.CompletedCategories.Contains(c.Category), history.CompletedTemplates.ContainsKey(c.TemplateId)));
                var quest = UserQuest.Offer(userId, item, reward, QuestSource.Daily, date, slot, utcNow, expires);
                quests.Add(quest);

                var affinity = persona.TrueAffinity(c.InterestIds.Select(id => catalog.InterestCodes[id]));
                var accepted = Decide(quest, c, affinity, persona, profile, scenario, rng, quests, scheduled, day, utcNow, run, progress);

                run.Offers.Add(new OfferRecord(day, c.Code, c.Category, affinity,
                    c.Type == QuestType.Daily || c.MaxMinutes <= 30, item.IsExploration, c.Effort > persona.Ability, accepted, slot));
            }
        }

        run.LifeLevel = progress.LifeLevel;
        var weights = profile.InterestWeights();
        run.HiddenInterestsDiscovered = persona.Hidden.Count(code =>
            weights.GetValueOrDefault(catalog.InterestIds[code]) >= 0.4 ||
            quests.Any(q => q.Status == QuestStatus.Completed && q.InterestIds.Contains(catalog.InterestIds[code])));

        return run;
    }

    private bool Decide(
        UserQuest quest, QuestCandidate c, double affinity, Persona persona, UserProfile profile, Scenario scenario,
        Random rng, List<UserQuest> quests, List<(UserQuest, int, double)> scheduled, int day, DateTime utcNow,
        UserRun run, PlayerProgress progress)
    {
        var tooExpensive = c.Cost > persona.Comfort;
        var tooLong = c.Type is QuestType.Daily or QuestType.Weekly
            ? c.MinMinutes > persona.SessionMinutes
            : c.MinMinutes > persona.WeeklyMinutes;
        var beyondAbility = c.Effort > persona.Ability;
        var activeCount = quests.Count(q => q.Status == QuestStatus.Accepted);

        var p = Math.Pow(affinity, 1.5) * 0.85;
        if (tooExpensive) p *= 0.2;
        if (tooLong) p *= 0.35;
        if (beyondAbility || activeCount >= 5) p = 0;

        if (rng.NextDouble() < p)
        {
            quest.Accept(utcNow);
            var completes = rng.NextDouble() < 0.55 + 0.4 * affinity;
            if (!completes)
                return true;

            var window = (int)UserQuest.CompletionWindow(c.Type).TotalDays;
            var delay = c.Type switch
            {
                QuestType.Daily => 0,
                QuestType.Weekly => rng.Next(1, Math.Min(5, window)),
                QuestType.Adventure => rng.Next(3, Math.Min(11, window)),
                _ => rng.Next(7, Math.Min(21, window))
            };

            if (delay == 0)
                CompleteQuest(quest, affinity, persona, profile, progress, run, scenario, rng, day, utcNow.AddMinutes(90));
            else
                scheduled.Add((quest, day + delay, affinity));
            return true;
        }

        if (rng.NextDouble() < 0.6)
        {
            var reason = affinity < 0.35 ? SkipReason.NotInterested
                : tooExpensive ? SkipReason.TooExpensive
                : tooLong ? SkipReason.NoTime
                : beyondAbility ? SkipReason.Other
                : SkipReason.NotToday;

            quest.Skip(reason, utcNow);
            if (reason == SkipReason.NotInterested && scenario.Learning)
                profile.AdjustInterests(quest.InterestIds, InterestLearning.NotInterestedDelta, utcNow);
        }

        return false;
    }

    private static void CompleteQuest(
        UserQuest quest, double affinity, Persona persona, UserProfile profile, PlayerProgress progress,
        UserRun run, Scenario scenario, Random rng, int day, DateTime utcNow)
    {
        var completion = quest.Complete(utcNow);
        if (!completion.Succeeded || !completion.Data)
            return;

        var isNewCategory = quest.Reward.NoveltyMultiplier == RewardCalculator.NewCategoryMultiplier;
        progress.ApplyQuestReward(quest, utcNow);
        if (scenario.Learning)
            profile.AdjustInterests(quest.InterestIds, InterestLearning.CompletionDelta, utcNow);

        int? rating = null;
        if (rng.NextDouble() < 0.7)
        {
            rating = Math.Clamp((int)Math.Round(1 + 4 * Math.Clamp(affinity + Gaussian(rng) * 0.12, 0, 1)), 1, 5);
            FeedbackPreference? preference = rating >= 5 && rng.NextDouble() < 0.35 ? FeedbackPreference.MoreLikeThis
                : rating <= 2 && rng.NextDouble() < 0.5 ? FeedbackPreference.LessLikeThis
                : null;

            var feedback = quest.RecordFeedback(rating, preference, utcNow);
            if (feedback.Data)
            {
                progress.RegisterFeedback(utcNow);
                if (scenario.Learning)
                {
                    var delta = InterestLearning.FromRating(rating.Value) + preference switch
                    {
                        FeedbackPreference.MoreLikeThis => InterestLearning.MoreLikeThisDelta,
                        FeedbackPreference.LessLikeThis => InterestLearning.LessLikeThisDelta,
                        _ => 0
                    };
                    profile.AdjustInterests(quest.InterestIds, delta, utcNow);
                }
            }
        }

        run.Completions.Add(new CompletionRecord(day, quest.Category, rating, isNewCategory));
    }

    private void ReactToStarterCards(Persona persona, UserProfile profile, DateTime utcNow)
    {
        foreach (var card in catalog.StarterCards)
        {
            var affinity = persona.TrueAffinity(card.InterestIds.Select(id => catalog.InterestCodes[id]));
            if (affinity >= 0.6)
                profile.AdjustInterests(card.InterestIds, InterestLearning.StarterLikeDelta, utcNow);
            else if (affinity <= 0.25)
                profile.AdjustInterests(card.InterestIds, InterestLearning.StarterDislikeDelta, utcNow);
        }
    }

    /// <summary>Repository'nin engine için hazırladığı görünümün aynısı (30 günlük pencere).</summary>
    private static RecommendationHistory BuildHistory(List<UserQuest> quests, PlayerProgress progress, DateTime utcNow)
    {
        var recent = quests
            .Where(q => q.OfferedAt >= utcNow.AddDays(-30))
            .Select(q => new QuestHistoryItem(
                q.TemplateId, q.Category, q.InterestIds, q.Status, q.OfferedAt,
                q.CompletedAt ?? q.SkippedAt ?? q.ExpiredAt ?? q.AcceptedAt, q.SkipReason, q.Rating, q.Preference));

        var open = quests.Where(q => q.IsOpen && q.ExpiresAt > utcNow).Select(q => q.TemplateId).Distinct();
        var completed = quests.Where(q => q.Status == QuestStatus.Completed)
            .GroupBy(q => q.TemplateId)
            .ToDictionary(g => g.Key, g => g.Max(q => q.CompletedAt!.Value));

        return new RecommendationHistory(recent, open, completed,
            progress.Categories.Where(c => c.CompletedCount > 0).Select(c => c.Category));
    }

    private static DateTime ToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Istanbul);

    private static double Gaussian(Random rng)
        => Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
}
