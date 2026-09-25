using System.Diagnostics;
using Gvn.GvnFramework.Core.Exceptions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Application.Narration;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Quests;

public sealed record QuestOffers(DateOnly Date, IReadOnlyList<UserQuest> Quests, string? Message);

/// <summary>
/// Öneri orkestrasyonu: profil, geçmiş ve katalogdan engine girdisini hazırlar, sonuçları ödülleri
/// hesaplanmış <see cref="UserQuest"/> snapshot'larına çevirir. Engine'in kendisi saf ve I/O'suzdur.
/// </summary>
public sealed class QuestOfferService(
    IUserProfileRepository profiles,
    IPlayerProgressRepository progressRepository,
    IUserQuestRepository quests,
    IQuestCatalog catalog,
    IUnitOfWork unitOfWork,
    IOptions<QuestOptions> questOptions,
    IRecommendationWeightsProvider weights,
    LifeQuestMetrics metrics,
    QuestNarrationService narration,
    TimeProvider clock,
    ILogger<QuestOfferService> logger)
{
    private const string NoMatchMessage =
        "Şu an tercihlerine uyan bir quest bulamadık. Bütçeni, şehrini veya ilgi alanlarını güncellemek yeni seçenekler açabilir.";

    private QuestOptions Options => questOptions.Value;

    /// <summary>Kullanıcının yerel günü için 3'lü günlük öneriyi döner; yoksa üretir. Tekrar çağrılabilir.</summary>
    public async Task<Result<QuestOffers>> GetOrCreateDailyOffersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<QuestOffers>.Fail(ProfileErrors.ProfileNotFound);
        if (!profile.OnboardingCompleted)
            return Result<QuestOffers>.Fail(ProfileErrors.OnboardingRequired);

        var (nowUtc, localNow, today, timeZone) = Now(profile);

        var existing = await quests.GetOffersAsync(userId, today, QuestSource.Daily, cancellationToken);
        if (existing.Count > 0)
            return Result<QuestOffers>.Ok(new QuestOffers(today, existing, null));

        var context = new RecommendationContext(localNow, nowUtc, Options.DailyOfferCount, Seed(userId, today));
        var offers = await CreateOffersAsync(profile, context, QuestSource.Daily, today, slotOffset: 0,
            TimeZones.EndOfLocalDayUtc(today, timeZone), cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            // Aynı gün için eşzamanlı üretim (ör. job ve kullanıcı isteği): benzersiz indeks ikinciyi reddeder.
            logger.LogInformation("Daily offers for {UserId} on {Date} were created concurrently; reloading.", userId, today);
            existing = await quests.GetOffersAsync(userId, today, QuestSource.Daily, cancellationToken);
            return Result<QuestOffers>.Ok(new QuestOffers(today, existing, null));
        }

        return Result<QuestOffers>.Ok(new QuestOffers(today, offers, offers.Count == 0 ? NoMatchMessage : null));
    }

    /// <summary>
    /// "Bu akşam 2 saatim var" senaryosu: bağlama göre yeni öneriler. Önceki kabul edilmemiş bağlamsal
    /// öneriler geri çekilir; gün başına tur sınırı vardır.
    /// </summary>
    public async Task<Result<QuestOffers>> SuggestAsync(
        Guid userId, int? availableMinutes, CostBand? maxCost, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<QuestOffers>.Fail(ProfileErrors.ProfileNotFound);
        if (!profile.OnboardingCompleted)
            return Result<QuestOffers>.Fail(ProfileErrors.OnboardingRequired);

        var (nowUtc, localNow, today, _) = Now(profile);

        var previous = await quests.GetOffersAsync(userId, today, QuestSource.OnDemand, cancellationToken);
        var round = previous.Select(q => q.Slot / 10).Distinct().Count();
        if (round >= Options.MaxSuggestionRoundsPerDay)
            return Result<QuestOffers>.Fail(QuestErrors.SuggestionLimitReached);

        foreach (var quest in previous)
            quest.Withdraw(nowUtc);

        var context = new RecommendationContext(
            localNow, nowUtc, Options.SuggestionCount, Seed(userId, today) + round + 1,
            availableMinutes, maxCost, RequireShortQuest: availableMinutes is null);

        var offers = await CreateOffersAsync(profile, context, QuestSource.OnDemand, today, slotOffset: round * 10,
            nowUtc.AddHours(Options.SuggestionTtlHours), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<QuestOffers>.Ok(new QuestOffers(today, offers, offers.Count == 0 ? NoMatchMessage : null));
    }

    /// <summary>
    /// "Sonra yaparım" listesinden başlatma: tek template motorun uygunluk filtrelerinden (cooldown, efor, açık
    /// görev…) geçirilir ve skor dökümüyle birlikte kabul edilmiş bir quest olarak oluşturulur.
    /// </summary>
    public async Task<Result<UserQuest>> StartTemplateAsync(Guid userId, Guid templateId, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<UserQuest>.Fail(ProfileErrors.ProfileNotFound);
        if (!profile.OnboardingCompleted)
            return Result<UserQuest>.Fail(ProfileErrors.OnboardingRequired);

        if (await quests.CountAcceptedAsync(userId, cancellationToken) >= Options.MaxActiveQuests)
            return Result<UserQuest>.Fail(QuestErrors.TooManyActiveQuests(Options.MaxActiveQuests));

        var (nowUtc, localNow, today, _) = Now(profile);
        var input = await LoadInputAsync(profile, nowUtc, cancellationToken);
        var candidate = input.Candidates.FirstOrDefault(c => c.TemplateId == templateId);
        if (candidate is null)
            return Result<UserQuest>.Fail(QuestErrors.TemplateUnavailable);

        var context = new RecommendationContext(localNow, nowUtc, 1, Seed(userId, today), RequireShortQuest: false);
        var result = new QuestRecommendationEngine(input.Weights.Weights)
            .Recommend([candidate], input.Profile, input.History, input.Graph, context);
        if (result.IsEmpty)
            return Result<UserQuest>.Fail(QuestErrors.NotOfferableNow(result.FilteredOut.Keys.FirstOrDefault() ?? string.Empty));

        var quest = await ToQuestAsync(result.Items[0], input, QuestSource.Saved, today, SavedSlot, nowUtc, nowUtc.AddHours(1), cancellationToken);
        var accepted = quest.Accept(nowUtc);
        if (!accepted.Succeeded)
            return Result<UserQuest>.Fail(accepted.Errors);

        await quests.AddAsync(quest, cancellationToken);
        metrics.Offered(QuestSource.Saved, 1);
        metrics.Accepted(quest.Category);
        return Result<UserQuest>.Ok(quest);
    }

    private const int SavedSlot = 100;

    private async Task<IReadOnlyList<UserQuest>> CreateOffersAsync(
        UserProfile profile, RecommendationContext context, QuestSource source, DateOnly offerDate,
        int slotOffset, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var input = await LoadInputAsync(profile, context.UtcNow, cancellationToken);

        var result = new QuestRecommendationEngine(input.Weights.Weights)
            .Recommend(input.Candidates, input.Profile, input.History, input.Graph, context);

        var offers = new List<UserQuest>(result.Items.Count);
        for (var i = 0; i < result.Items.Count; i++)
        {
            var quest = await ToQuestAsync(result.Items[i], input, source, offerDate, slotOffset + i, context.UtcNow,
                expiresAtUtc, cancellationToken);
            await quests.AddAsync(quest, cancellationToken);
            offers.Add(quest);
        }

        metrics.Offered(source, offers.Count);
        metrics.RecommendationGenerated(stopwatch.Elapsed.TotalMilliseconds, result.IsEmpty);

        if (result.IsEmpty)
            logger.LogInformation(
                "No quest could be recommended for {UserId}. Candidates={Candidates} Filtered={@Filtered}",
                profile.UserId, result.CandidateCount, result.FilteredOut);

        return offers;
    }

    private async Task<EngineInput> LoadInputAsync(UserProfile profile, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var userId = profile.UserId;
        var candidates = await catalog.GetOfferableCandidatesAsync(cancellationToken);
        var graph = await catalog.GetTasteGraphAsync(cancellationToken);
        var progress = await progressRepository.GetByUserIdAsync(userId, cancellationToken);
        var completedTemplates = await quests.GetCompletedTemplatesAsync(userId, cancellationToken);
        var openTemplates = await quests.GetOpenTemplateIdsAsync(userId, nowUtc, cancellationToken);
        var recent = await quests.GetHistoryItemsAsync(userId, nowUtc.AddDays(-Options.HistoryWindowDays), cancellationToken);
        var loved = await quests.GetLovedTemplateIdsAsync(userId, cancellationToken);

        var completedCategories = progress?.Categories.Where(c => c.CompletedCount > 0).Select(c => c.Category) ?? [];
        var history = new RecommendationHistory(recent, openTemplates, completedTemplates, completedCategories, loved);

        var recommendationProfile = new RecommendationProfile(
            profile.DiscoveryRadius, profile.Budget, profile.WeeklyAvailableMinutes,
            profile.Goals.ToHashSet(), profile.InterestWeights(), profile.City is not null, profile.MaxPhysicalEffort);

        return new EngineInput(userId, candidates, graph, history, recommendationProfile,
            await weights.GetForUserAsync(userId, cancellationToken));
    }

    private async Task<UserQuest> ToQuestAsync(
        RecommendedQuest item, EngineInput input, QuestSource source, DateOnly offerDate, int slot,
        DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var candidate = item.Candidate;
        var novelty = RewardCalculator.NoveltyMultiplier(
            input.History.CompletedCategories.Contains(candidate.Category),
            input.History.CompletedTemplates.ContainsKey(candidate.TemplateId));
        var reward = RewardCalculator.Calculate(
            candidate.Type, candidate.Difficulty, candidate.SecondaryCategory is not null, novelty);

        var topics = candidate.InterestIds.Select(input.Graph.NameOf).ToList();
        var text = await narration.NarrateAsync(candidate, topics, cancellationToken);

        var quest = UserQuest.Offer(input.UserId, item, reward, source, offerDate, slot, nowUtc, expiresAtUtc, text);
        if (input.Weights is { ExperimentId: { } experimentId, Variant: { } variant })
            quest.AssignExperiment(experimentId, variant);
        return quest;
    }

    private sealed record EngineInput(
        Guid UserId,
        IReadOnlyList<QuestCandidate> Candidates,
        TasteGraph Graph,
        RecommendationHistory History,
        RecommendationProfile Profile,
        UserWeights Weights);

    private (DateTime NowUtc, DateTime LocalNow, DateOnly Today, TimeZoneInfo TimeZone) Now(UserProfile profile)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var timeZone = profile.ResolveTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        return (nowUtc, localNow, DateOnly.FromDateTime(localNow), timeZone);
    }

    /// <summary>Kullanıcı + gün için süreçten bağımsız, tekrarlanabilir tohum (HashCode.Combine süreç başına rastgeledir).</summary>
    private static int Seed(Guid userId, DateOnly date)
        => BitConverter.ToInt32(userId.ToByteArray(), 0) ^ date.DayNumber;
}
