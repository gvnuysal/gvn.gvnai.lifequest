using System.Diagnostics;
using Gvn.GvnFramework.Core.Exceptions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Diagnostics;
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
    IOptions<RecommendationWeights> weights,
    LifeQuestMetrics metrics,
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

    private async Task<IReadOnlyList<UserQuest>> CreateOffersAsync(
        UserProfile profile, RecommendationContext context, QuestSource source, DateOnly offerDate,
        int slotOffset, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = profile.UserId;

        var candidates = await catalog.GetOfferableCandidatesAsync(cancellationToken);
        var graph = await catalog.GetTasteGraphAsync(cancellationToken);
        var progress = await progressRepository.GetByUserIdAsync(userId, cancellationToken);
        var completedTemplates = await quests.GetCompletedTemplatesAsync(userId, cancellationToken);
        var openTemplates = await quests.GetOpenTemplateIdsAsync(userId, context.UtcNow, cancellationToken);
        var recent = await quests.GetHistoryItemsAsync(
            userId, context.UtcNow.AddDays(-Options.HistoryWindowDays), cancellationToken);

        var completedCategories = progress?.Categories.Where(c => c.CompletedCount > 0).Select(c => c.Category) ?? [];
        var history = new RecommendationHistory(recent, openTemplates, completedTemplates, completedCategories);

        var recommendationProfile = new RecommendationProfile(
            profile.DiscoveryRadius, profile.Budget, profile.WeeklyAvailableMinutes,
            profile.Goals.ToHashSet(), profile.InterestWeights(), profile.City is not null);

        var result = new QuestRecommendationEngine(weights.Value)
            .Recommend(candidates, recommendationProfile, history, graph, context);

        var offers = new List<UserQuest>(result.Items.Count);
        for (var i = 0; i < result.Items.Count; i++)
        {
            var item = result.Items[i];
            var candidate = item.Candidate;

            var novelty = RewardCalculator.NoveltyMultiplier(
                history.CompletedCategories.Contains(candidate.Category),
                history.CompletedTemplates.ContainsKey(candidate.TemplateId));
            var reward = RewardCalculator.Calculate(
                candidate.Type, candidate.Difficulty, candidate.SecondaryCategory is not null, novelty);

            var quest = UserQuest.Offer(userId, item, reward, source, offerDate, slotOffset + i, context.UtcNow, expiresAtUtc);
            await quests.AddAsync(quest, cancellationToken);
            offers.Add(quest);
        }

        metrics.Offered(source, offers.Count);
        metrics.RecommendationGenerated(stopwatch.Elapsed.TotalMilliseconds, result.IsEmpty);

        if (result.IsEmpty)
            logger.LogInformation(
                "No quest could be recommended for {UserId}. Candidates={Candidates} Filtered={@Filtered}",
                userId, result.CandidateCount, result.FilteredOut);

        return offers;
    }

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
