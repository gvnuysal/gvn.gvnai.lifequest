using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Quests;

/// <summary>
/// Belirli bir kullanıcıya belirli bir anda sunulmuş quest snapshot'ı. Başlık, ödül, zorluk ve öneri
/// gerekçesi burada donar; template sonradan değişse de geçmiş bozulmaz. State geçişleri yalnızca bu
/// aggregate üzerinden yapılır.
/// </summary>
public sealed class UserQuest : AggregateRoot
{
    public Guid UserId { get; private set; }

    // ── Template snapshot ────────────────────────────────────────────────────
    public Guid TemplateId { get; private set; }
    public string TemplateCode { get; private set; } = default!;
    public int TemplateVersion { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public QuestType Type { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public LifeCategory Category { get; private set; }
    public LifeCategory? SecondaryCategory { get; private set; }
    public int MinMinutes { get; private set; }
    public int MaxMinutes { get; private set; }
    public CostBand Cost { get; private set; }
    public List<Guid> InterestIds { get; private set; } = [];
    public QuestReward Reward { get; private set; } = default!;
    public PhysicalEffort Effort { get; private set; }

    /// <summary>Başlık/açıklama template'ten mi yoksa guardrail'den geçmiş AI anlatımından mı geldi.</summary>
    public NarrationSource NarrationSource { get; private set; } = NarrationSource.Template;

    // ── Öneri snapshot'ı (açıklanabilirlik) ──────────────────────────────────
    public QuestSource Source { get; private set; }
    public DateOnly OfferDate { get; private set; }
    public int Slot { get; private set; }
    public ScoreBreakdown Score { get; private set; } = ScoreBreakdown.Empty;
    public bool IsExploration { get; private set; }
    public List<string> ReasonCodes { get; private set; } = [];
    public string Explanation { get; private set; } = default!;

    // ── İngilizce kopya: teklif anında iki dil birden saklanır; eski kayıtlarda boştur (Türkçeye düşer) ──
    public string? TitleEn { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? ExplanationEn { get; private set; }

    public Localization.LocalizedText LocalizedTitle => Localization.LocalizedText.WithFallback(Title, TitleEn);
    public Localization.LocalizedText LocalizedDescription => Localization.LocalizedText.WithFallback(Description, DescriptionEn);
    public Localization.LocalizedText LocalizedExplanation => Localization.LocalizedText.WithFallback(Explanation, ExplanationEn);

    // ── Yaşam döngüsü ────────────────────────────────────────────────────────
    public QuestStatus Status { get; private set; }
    public DateTime OfferedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? SkippedAt { get; private set; }
    public DateTime? ExpiredAt { get; private set; }
    public SkipReason? SkipReason { get; private set; }

    /// <summary>Kullanıcının kendi takvimine koyduğu zaman (UTC). Hatırlatma değildir; yalnızca planlama.</summary>
    public DateTime? PlannedAt { get; private set; }

    // ── A/B deneyi ───────────────────────────────────────────────────────────
    /// <summary>Öneri üretildiği anda çalışan deney ve kullanıcının grubu; sonuçlar bu alanlardan hesaplanır.</summary>
    public Guid? ExperimentId { get; private set; }
    public ExperimentVariant? ExperimentVariant { get; private set; }

    // ── Geri bildirim ────────────────────────────────────────────────────────
    public int? Rating { get; private set; }
    public FeedbackPreference? Preference { get; private set; }
    public DateTime? FeedbackAt { get; private set; }

    private UserQuest() { }

    public static UserQuest Offer(
        Guid userId,
        RecommendedQuest recommendation,
        QuestReward reward,
        QuestSource source,
        DateOnly offerDate,
        int slot,
        DateTime nowUtc,
        DateTime expiresAtUtc,
        QuestText? narration = null)
    {
        var candidate = recommendation.Candidate;

        return new UserQuest
        {
            UserId = userId,
            TemplateId = candidate.TemplateId,
            TemplateCode = candidate.Code,
            TemplateVersion = candidate.Version,
            Title = narration?.Title ?? candidate.Title,
            Description = narration?.Description ?? candidate.Description,
            NarrationSource = narration?.Source ?? NarrationSource.Template,
            Effort = candidate.Effort,
            Type = candidate.Type,
            Difficulty = candidate.Difficulty,
            Category = candidate.Category,
            SecondaryCategory = candidate.SecondaryCategory,
            MinMinutes = candidate.MinMinutes,
            MaxMinutes = candidate.MaxMinutes,
            Cost = candidate.Cost,
            InterestIds = candidate.InterestIds.ToList(),
            Reward = reward,
            Source = source,
            OfferDate = offerDate,
            Slot = slot,
            Score = recommendation.Score,
            IsExploration = recommendation.IsExploration,
            ReasonCodes = recommendation.Reasons.Select(r => r.Code.ToString()).ToList(),
            Explanation = recommendation.Explanation.Tr,
            ExplanationEn = recommendation.Explanation.En,
            TitleEn = candidate.TitleEn,
            DescriptionEn = candidate.DescriptionEn,
            Status = QuestStatus.Offered,
            OfferedAt = nowUtc,
            ExpiresAt = expiresAtUtc
        };
    }

    public bool IsOpen => Status is QuestStatus.Offered or QuestStatus.Accepted;

    /// <summary>Kabul edildikten sonra tamamlamak için tanınan süre. Kaçırmanın cezası yoktur.</summary>
    public static TimeSpan CompletionWindow(QuestType type) => type switch
    {
        QuestType.Daily => TimeSpan.FromDays(1),
        QuestType.Weekly => TimeSpan.FromDays(7),
        QuestType.Adventure => TimeSpan.FromDays(14),
        QuestType.Epic => TimeSpan.FromDays(30),
        _ => TimeSpan.FromDays(7)
    };

    /// <summary>Deney grubunu öneri anında kaydeder.</summary>
    public void AssignExperiment(Guid experimentId, ExperimentVariant variant)
    {
        ExperimentId = experimentId;
        ExperimentVariant = variant;
    }

    /// <summary>Kabul edilmiş quest'i tamamlama süresi içinde bir zamana planlar; <c>null</c> planı kaldırır.</summary>
    public Result Plan(DateTime? plannedAtUtc, DateTime nowUtc)
    {
        if (Status != QuestStatus.Accepted)
            return Result.Fail(QuestErrors.PlanRequiresAccepted);

        if (plannedAtUtc is { } at && (at < nowUtc.AddMinutes(-5) || at >= ExpiresAt))
            return Result.Fail(QuestErrors.PlanOutsideWindow(ExpiresAt));

        PlannedAt = plannedAtUtc;
        return Result.Ok();
    }

    public Result Accept(DateTime nowUtc)
    {
        if (Status == QuestStatus.Accepted)
            return Result.Ok();

        if (Status != QuestStatus.Offered)
            return Result.Fail(QuestErrors.InvalidTransition(Status, "accept"));

        if (IsPastDeadline(nowUtc))
            return Result.Fail(QuestErrors.Expired);

        Status = QuestStatus.Accepted;
        AcceptedAt = nowUtc;
        ExpiresAt = nowUtc.Add(CompletionWindow(Type));
        AddDomainEvent(new QuestAcceptedEvent(Id, UserId));
        return Result.Ok();
    }

    /// <summary>
    /// Idempotent: zaten tamamlanmış quest için tekrar çağrı başarılı döner ama <c>false</c> ile işaretlenir,
    /// böylece çağıran taraf ödülü ikinci kez vermez.
    /// </summary>
    /// <returns>Bu çağrıyla yeni tamamlandıysa <c>true</c>.</returns>
    public Result<bool> Complete(DateTime nowUtc)
    {
        if (Status == QuestStatus.Completed)
            return Result<bool>.Ok(false);

        if (Status == QuestStatus.Offered)
            return Result<bool>.Fail(QuestErrors.MustAcceptFirst);

        if (Status != QuestStatus.Accepted)
            return Result<bool>.Fail(QuestErrors.InvalidTransition(Status, "complete"));

        if (IsPastDeadline(nowUtc))
            return Result<bool>.Fail(QuestErrors.Expired);

        Status = QuestStatus.Completed;
        CompletedAt = nowUtc;
        AddDomainEvent(new QuestCompletedEvent(Id, UserId, Category, Reward.LifeXp));
        return Result<bool>.Ok(true);
    }

    public Result Skip(SkipReason reason, DateTime nowUtc)
    {
        if (Status == QuestStatus.Skipped)
            return Result.Ok();

        if (!IsOpen)
            return Result.Fail(QuestErrors.InvalidTransition(Status, "skip"));

        Status = QuestStatus.Skipped;
        SkipReason = reason;
        SkippedAt = nowUtc;
        AddDomainEvent(new QuestSkippedEvent(Id, UserId, reason));
        return Result.Ok();
    }

    /// <summary>Süresi geçmiş açık quest'i kapatır. Job'lar tekrar çalışsa da güvenlidir.</summary>
    public bool TryExpire(DateTime nowUtc)
    {
        if (!IsOpen || !IsPastDeadline(nowUtc))
            return false;

        Status = QuestStatus.Expired;
        ExpiredAt = nowUtc;
        return true;
    }

    /// <summary>Yeni bağlamsal öneri istendiğinde kabul edilmemiş eski önerileri geri çeker.</summary>
    public bool Withdraw(DateTime nowUtc)
    {
        if (Status != QuestStatus.Offered)
            return false;

        Status = QuestStatus.Expired;
        ExpiredAt = nowUtc;
        return true;
    }

    /// <returns>Bu çağrıyla ilk kez puan verildiyse <c>true</c>.</returns>
    public Result<bool> RecordFeedback(int? rating, FeedbackPreference? preference, DateTime nowUtc)
    {
        var firstRating = false;

        if (rating is not null)
        {
            if (Status != QuestStatus.Completed)
                return Result<bool>.Fail(QuestErrors.RatingRequiresCompletion);

            if (rating is < 1 or > 5)
                return Result<bool>.Fail(QuestErrors.InvalidRating);

            firstRating = Rating is null;
            Rating = rating;
        }

        if (preference is not null)
            Preference = preference;

        FeedbackAt = nowUtc;
        return Result<bool>.Ok(firstRating);
    }

    private bool IsPastDeadline(DateTime nowUtc) => nowUtc >= ExpiresAt;
}
