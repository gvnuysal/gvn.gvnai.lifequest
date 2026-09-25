using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Community;

/// <summary>
/// Kullanıcının önerdiği deneyim fikri. Admin inceler; kabul edilirse düzenlenip katalog template'ine dönüşür.
/// Template kullanıcıya referans taşımaz: hesap silinse de katalog içeriği kalır, fikir kaydı ise silinir.
/// </summary>
public sealed class QuestIdea : AggregateRoot
{
    public const int MaxPendingPerUser = 3;
    public const int MaxPerDay = 5;

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public LifeCategory Category { get; private set; }
    public int Minutes { get; private set; }
    public CostBand Cost { get; private set; }
    public bool IsOutdoor { get; private set; }
    public IdeaStatus Status { get; private set; } = IdeaStatus.Pending;

    /// <summary>Otomatik taramanın admin için işaretledikleri (ör. riskli ifade); kaydı engellemez.</summary>
    public List<string> Flags { get; private set; } = [];

    public string? ReviewNote { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public Guid? TemplateId { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    private QuestIdea() { }

    public static QuestIdea Submit(
        Guid userId, string title, string description, LifeCategory category, int minutes, CostBand cost, bool isOutdoor,
        IEnumerable<string> flags, DateTime nowUtc) => new()
    {
        UserId = userId,
        Title = Guard.NotNullOrWhiteSpace(title, nameof(title)).Trim(),
        Description = Guard.NotNullOrWhiteSpace(description, nameof(description)).Trim(),
        Category = category,
        Minutes = Guard.InRange(minutes, 5, 600, nameof(minutes)),
        Cost = cost,
        IsOutdoor = isOutdoor,
        Flags = flags.Distinct().ToList(),
        SubmittedAt = nowUtc
    };

    public Result Accept(Guid templateId, string reviewer, string? note, DateTime nowUtc)
    {
        if (Status != IdeaStatus.Pending)
            return Result.Fail(IdeaErrors.AlreadyReviewed);

        Status = IdeaStatus.Accepted;
        TemplateId = templateId;
        Review(reviewer, note, nowUtc);
        return Result.Ok();
    }

    public Result Reject(string reviewer, string note, DateTime nowUtc)
    {
        if (Status != IdeaStatus.Pending)
            return Result.Fail(IdeaErrors.AlreadyReviewed);

        Status = IdeaStatus.Rejected;
        Review(reviewer, Guard.NotNullOrWhiteSpace(note, nameof(note)), nowUtc);
        return Result.Ok();
    }

    private void Review(string reviewer, string? note, DateTime nowUtc)
    {
        ReviewedBy = reviewer;
        ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ReviewedAt = nowUtc;
    }
}

public enum IdeaStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3
}

public static class IdeaErrors
{
    public static readonly Error NotFound = Error.NotFound("IDEA_NOT_FOUND", "Fikir bulunamadı.");

    public static readonly Error AlreadyReviewed = Error.Conflict("IDEA_ALREADY_REVIEWED", "Bu fikir zaten değerlendirildi.");

    public static readonly Error TooManyPending = Error.Conflict("IDEA_TOO_MANY_PENDING",
        $"Değerlendirme bekleyen en fazla {QuestIdea.MaxPendingPerUser} fikrin olabilir. Önceki fikirlerin incelenince yenisini gönderebilirsin.");

    public static readonly Error DailyLimit = Error.Conflict("IDEA_DAILY_LIMIT",
        $"Bir günde en fazla {QuestIdea.MaxPerDay} fikir gönderebilirsin.");

    public static readonly Error NoLinksOrContacts = Error.Validation("Description",
        "Fikirde bağlantı, e-posta veya telefon numarası olmamalı. Deneyimi kendi cümlelerinle anlat.");
}

public interface IQuestIdeaRepository : IRepository<QuestIdea>
{
    Task<IReadOnlyList<QuestIdea>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountSubmittedSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<QuestIdea> Items, int TotalCount)> GetPageAsync(
        IdeaStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(IdeaStatus status, CancellationToken cancellationToken = default);
}
