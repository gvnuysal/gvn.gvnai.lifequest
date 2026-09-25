using Gvn.GvnFramework.Domain.Entities;
using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Quests;

/// <summary>
/// "Sonra yaparım" listesi: kabul edilmeyen bir öneri kaybolmasın diye template'i kaydeder. Görev snapshot'ı
/// değil template referansı tutulur; başlatıldığında o anki tanım ve ödülle yeni bir quest oluşur.
/// </summary>
public sealed class SavedQuest : Entity
{
    public const int MaxPerUser = 30;

    public Guid UserId { get; private set; }
    public Guid TemplateId { get; private set; }
    public DateTime SavedAt { get; private set; }

    private SavedQuest() { }

    public static SavedQuest Create(Guid userId, Guid templateId, DateTime nowUtc)
        => new() { UserId = userId, TemplateId = templateId, SavedAt = nowUtc };
}

public interface ISavedQuestRepository : IRepository<SavedQuest>
{
    Task<IReadOnlyList<SavedQuest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<SavedQuest?> GetAsync(Guid userId, Guid templateId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default);
}
