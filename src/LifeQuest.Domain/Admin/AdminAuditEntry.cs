using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Domain.Entities;

namespace LifeQuest.Domain.Admin;

/// <summary>
/// Admin işlemlerinin değiştirilemez kaydı. İşlemle aynı unit of work içinde yazılır; biri olmadan diğeri commit edilmez.
/// Kullanıcıya ait bir kayıt değildir, bu yüzden hesap silinse de kalır; silinen hesabın e-postası maskelenir.
/// </summary>
public sealed class AdminAuditEntry : Entity
{
    public Guid ActorId { get; private set; }
    public string ActorEmail { get; private set; } = default!;
    public AdminAction Action { get; private set; }
    public AdminTargetType TargetType { get; private set; }
    public Guid? TargetId { get; private set; }
    public string TargetLabel { get; private set; } = default!;
    public string? Reason { get; private set; }

    /// <summary>JSON: önce/sonra farkı veya işleme özgü ayrıntı.</summary>
    public string? Details { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private AdminAuditEntry() { }

    public static AdminAuditEntry Create(
        Guid actorId, string actorEmail, AdminAction action, AdminTargetType targetType, Guid? targetId,
        string targetLabel, string? reason, string? details, DateTime nowUtc) => new()
    {
        ActorId = actorId,
        ActorEmail = Guard.NotNullOrWhiteSpace(actorEmail, nameof(actorEmail)),
        Action = action,
        TargetType = targetType,
        TargetId = targetId,
        TargetLabel = Guard.NotNullOrWhiteSpace(targetLabel, nameof(targetLabel)),
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        Details = details,
        CreatedAt = nowUtc
    };
}

public enum AdminAction
{
    UserSuspended = 1,
    UserUnsuspended = 2,
    UserDeleted = 3,
    UserRoleChanged = 4,
    TemplateCreated = 10,
    TemplateUpdated = 11,
    TemplateSafetyChanged = 12,
    TemplateActivated = 13,
    TemplateDeactivated = 14,
    WeightsUpdated = 20,
    WeightsReset = 21
}

public enum AdminTargetType
{
    User = 1,
    QuestTemplate = 2,
    RecommendationSettings = 3
}

public static class EmailMask
{
    /// <summary><c>ayse@example.com</c> → <c>a***@example.com</c>. Silinen hesaplar için denetim kaydında kullanılır.</summary>
    public static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
